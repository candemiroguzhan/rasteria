using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.IO;
using Rasteria.Core.Rasters;
using Rasteria.Core.Scene;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using Serilog;
using SharpDX;
using SharpDX.DXGI;
using HelixDiffuseMaterial = HelixToolkit.Wpf.SharpDX.DiffuseMaterial;
using WpfVector3D = System.Windows.Media.Media3D.Vector3D;

namespace Rasteria.Rendering.Scene;

public sealed class SceneViewport3D :
    UserControl,
    ISceneRenderer,
    IDisposable
{
    private readonly Viewport3DX _viewport;
    private readonly Dictionary<Guid, List<Element3D>> _visualsByLayer = [];
    private readonly Dictionary<Guid, SceneBounds> _boundsByLayer = [];
    private readonly DefaultEffectsManager _effectsManager = new();
    private SceneBounds _sceneBounds = SceneBounds.Empty;
    private double _originX;
    private double _originY;
    private double _originZ;
    private bool _originInitialized;
    private bool _disposed;

    public SceneViewport3D()
    {
        ClipToBounds = true;

        var useGpuRendering = HasHardwareGpu();
        Log.Information("GPU rendering mode: {RenderMode}", useGpuRendering ? "Hardware adapter detected" : "No hardware adapter detected");

        _viewport = new Viewport3DX
        {
            ClipToBounds = true,
            EffectsManager = _effectsManager,
            BackgroundColor = System.Windows.Media.Color.FromRgb(36, 42, 50),
            ShowFrameRate = true,
            ShowCoordinateSystem = false,
            Camera = CreateTopDownCamera(SceneBounds.Empty)
        };

        Content = _viewport;
    }

    public Task SetSceneAsync(SceneModel scene, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (!Dispatcher.CheckAccess())
        {
            return Dispatcher.InvokeAsync(() => SetSceneAsync(scene, cancellationToken)).Task.Unwrap();
        }

        return SetSceneCoreAsync(scene, cancellationToken);
    }

    public Task AddLayerAsync(SceneLayer layer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (!Dispatcher.CheckAccess())
        {
            return Dispatcher.InvokeAsync(() => AddLayerAsync(layer, cancellationToken)).Task.Unwrap();
        }

        return AddLayerCoreAsync(layer, cancellationToken);
    }

    public void RemoveLayer(Guid layerId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => RemoveLayer(layerId));
            return;
        }

        if (!_visualsByLayer.Remove(layerId, out var visuals))
        {
            return;
        }

        ClearLayerVisuals(visuals);
        _boundsByLayer.Remove(layerId);
        RecalculateSceneBounds();
        Log.Information("Layer removed. Id {LayerId}, visual count {VisualCount}", layerId, visuals.Count);
    }

    public void SetLayerVisibility(Guid layerId, bool isVisible)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => SetLayerVisibility(layerId, isVisible));
            return;
        }

        if (!_visualsByLayer.TryGetValue(layerId, out var visuals))
        {
            return;
        }

        foreach (var visual in visuals)
        {
            visual.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
        }
    }

    public void SetLayerOpacity(Guid layerId, double opacity)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => SetLayerOpacity(layerId, opacity));
            return;
        }

        if (!_visualsByLayer.TryGetValue(layerId, out var visuals))
        {
            return;
        }

        foreach (var mesh in visuals.OfType<MeshGeometryModel3D>())
        {
            if (mesh.Material is PhongMaterial material)
            {
                material.DiffuseColor = SceneViewportRasterMath.WithAlpha(material.DiffuseColor, opacity);
            }
            else if (mesh.Material is HelixDiffuseMaterial diffuseMaterial)
            {
                diffuseMaterial.DiffuseColor = SceneViewportRasterMath.WithAlpha(diffuseMaterial.DiffuseColor, opacity);
            }
        }
    }

    public void FitLayer(Guid layerId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => FitLayer(layerId));
            return;
        }

        if (!_boundsByLayer.TryGetValue(layerId, out var bounds))
        {
            FitScene();
            return;
        }

        SetCameraForBounds(bounds);
    }

    public void FitScene()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(FitScene);
            return;
        }

        SetCameraForBounds(_sceneBounds);
    }

    public void SetCameraMode(SceneCameraMode mode)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => SetCameraMode(mode));
            return;
        }

        if (mode is not SceneCameraMode.TopDownOrthographic)
        {
            Log.Warning("Scene camera mode {CameraMode} is not supported. Rasteria supports top-down orthographic raster viewing only.", mode);
        }

        SetCameraForBounds(_sceneBounds);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (Dispatcher.CheckAccess())
        {
            ClearSceneResources();
            _effectsManager.Dispose();
        }
        else
        {
            Dispatcher.Invoke(() =>
            {
                ClearSceneResources();
                _effectsManager.Dispose();
            });
        }

        Log.Information("Scene resources disposed.");
    }

    private async Task SetSceneCoreAsync(SceneModel scene, CancellationToken cancellationToken)
    {
        ClearSceneResources();
        Log.Information("Scene cleared.");

        foreach (var layer in scene.Layers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await AddLayerCoreAsync(layer, cancellationToken);
        }
    }

    private async Task AddLayerCoreAsync(SceneLayer layer, CancellationToken cancellationToken)
    {
        if (layer is not RasterSceneLayer rasterLayer)
        {
            throw new NotSupportedException(
                $"Scene layer type '{layer.GetType().Name}' is not supported. Rasteria supports raster layers only.");
        }

        await AddRasterLayerAsync(rasterLayer, cancellationToken);
    }

    private async Task AddRasterLayerAsync(RasterSceneLayer layer, CancellationToken cancellationToken)
    {
        var metadata = layer.RasterLayer.RasterMetadata;
        Log.Information("Raster layer loading started. Path {Path}, layer {LayerName}", metadata.FilePath, layer.Name);

        EnsureOrigin(layer.Bounds);

        var viewport = new ViewportState(0, 0, metadata.Width, metadata.Height, 0.125, metadata.Width, metadata.Height);
        var coordinates = await layer.TileSource.GetVisibleTilesAsync(viewport, cancellationToken);
        Log.Information("Visible tile count {VisibleTileCount}. Path {Path}", coordinates.Count, metadata.FilePath);

        var visuals = new List<Element3D>();
        var loadedTiles = 0;
        var failedTiles = 0;

        try
        {
            foreach (var coordinate in coordinates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var tile = await layer.TileSource.GetTileAsync(coordinate, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    var model = CreateTexturedRasterTile(metadata, tile, layer.RasterLayer.Opacity);
                    visuals.Add(model);
                    _viewport.Items.Add(model);
                    loadedTiles++;
                    Log.Debug("Raster tile rendered. Layer {LayerName}, tile L{Level} X{X} Y{Y}, bitmap {Width}x{Height}, source {SourceX},{SourceY} {SourceWidth}x{SourceHeight}",
                        layer.Name,
                        coordinate.Level,
                        coordinate.X,
                        coordinate.Y,
                        tile.PixelWidth,
                        tile.PixelHeight,
                        tile.SourceX,
                        tile.SourceY,
                        tile.SourceWidth,
                        tile.SourceHeight);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    failedTiles++;
                    Log.Warning(exception, "Raster scene tile failed for {Tile}", coordinate);
                    var debug = CreateRasterDebugTile(metadata, coordinate, layer.RasterLayer.Opacity);
                    if (debug is not null)
                    {
                        visuals.Add(debug);
                        _viewport.Items.Add(debug);
                    }
                }
            }

            if (visuals.Count == 0)
            {
                var fallback = CreateBoundsPlane(ToLocalBounds(layer.Bounds), new Color4(0.2f, 0.5f, 0.9f, 0.8f));
                visuals.Add(fallback);
                _viewport.Items.Add(fallback);
            }

            var localBounds = ToLocalBounds(layer.Bounds);
            if (SceneViewportRasterMath.SanitizeBounds(localBounds) == SceneViewportRasterMath.DefaultBounds)
            {
                Log.Warning("Invalid raster bounds. Path {Path}, bounds {Bounds}, local bounds {LocalBounds}", metadata.FilePath, layer.Bounds, localBounds);
            }

            _visualsByLayer[layer.Id] = visuals;
            _boundsByLayer[layer.Id] = localBounds;
            RecalculateSceneBounds();
            SetCameraForBounds(localBounds);

            Log.Information("Raster layer loaded. Path {Path}, loaded tile count {LoadedTileCount}, failed tile count {FailedTileCount}, bounds {Bounds}, local bounds {LocalBounds}",
                metadata.FilePath,
                loadedTiles,
                failedTiles,
                layer.Bounds,
                localBounds);
        }
        catch (OperationCanceledException)
        {
            var cleanupCount = CleanupPartialVisuals(visuals);
            _boundsByLayer.Remove(layer.Id);
            _visualsByLayer.Remove(layer.Id);
            RecalculateSceneBounds();
            Log.Information("Raster layer loading cancelled. Path {Path}, partial visual cleanup count {CleanupCount}", metadata.FilePath, cleanupCount);
            throw;
        }
        catch (Exception exception)
        {
            var cleanupCount = CleanupPartialVisuals(visuals);
            _boundsByLayer.Remove(layer.Id);
            _visualsByLayer.Remove(layer.Id);
            RecalculateSceneBounds();
            Log.Warning(exception, "Raster layer loading failed for {Path}. Partial visual cleanup count {CleanupCount}", metadata.FilePath, cleanupCount);
            throw;
        }
    }

    private MeshGeometryModel3D CreateTexturedRasterTile(RasterMetadata metadata, RasterTile tile, double opacity)
    {
        try
        {
            SceneViewportRasterMath.ValidateTileBuffer(tile);
        }
        catch (InvalidDataException exception)
        {
            Log.Warning(exception, "Invalid tile buffer. Path {Path}, coordinate {Coordinate}", metadata.FilePath, tile.Coordinate);
            throw;
        }

        var corners = SceneViewportRasterMath.GetTileWorldCorners(metadata, tile);
        var geometry = CreateQuadGeometry(
            ToLocalVector(corners.TopLeft, 0),
            ToLocalVector(corners.TopRight, 0),
            ToLocalVector(corners.BottomRight, 0),
            ToLocalVector(corners.BottomLeft, 0));
        var texture = new TextureModel(tile.Bgra32Pixels, Format.B8G8R8A8_UNorm, tile.PixelWidth, tile.PixelHeight);
        var material = new HelixDiffuseMaterial
        {
            DiffuseColor = new Color4(1f, 1f, 1f, (float)Math.Clamp(opacity, 0, 1)),
            DiffuseMap = texture,
            EnableUnLit = true
        };

        return new MeshGeometryModel3D
        {
            Geometry = geometry,
            Material = material,
            CullMode = SharpDX.Direct3D11.CullMode.None
        };
    }

    private MeshGeometryModel3D? CreateRasterDebugTile(RasterMetadata metadata, TileCoordinate coordinate, double opacity)
    {
        if (!SceneViewportRasterMath.TryCalculateTileSourceRect(metadata, coordinate, out var rect))
        {
            Log.Debug("Skipping invalid debug raster tile. Path {Path}, coordinate {Coordinate}", metadata.FilePath, coordinate);
            return null;
        }

        var corners = SceneViewportRasterMath.GetWorldCorners(
            metadata.GeoTransform,
            rect.SourceX,
            rect.SourceY,
            rect.SourceWidth,
            rect.SourceHeight);
        var material = new HelixDiffuseMaterial
        {
            DiffuseColor = new Color4(1f, 1f, 1f, (float)Math.Clamp(opacity, 0, 1)),
            DiffuseMap = CreateCheckerTexture(),
            EnableUnLit = true
        };

        return new MeshGeometryModel3D
        {
            Geometry = CreateQuadGeometry(
                ToLocalVector(corners.TopLeft, 0.1f),
                ToLocalVector(corners.TopRight, 0.1f),
                ToLocalVector(corners.BottomRight, 0.1f),
                ToLocalVector(corners.BottomLeft, 0.1f)),
            Material = material,
            CullMode = SharpDX.Direct3D11.CullMode.None
        };
    }

    private static MeshGeometryModel3D CreateBoundsPlane(SceneBounds bounds, Color4 color)
    {
        var safeBounds = SceneViewportRasterMath.SanitizeBounds(bounds);
        return new MeshGeometryModel3D
        {
            Geometry = CreateQuadGeometry(
                new Vector3((float)safeBounds.MinX, (float)safeBounds.MaxY, (float)safeBounds.MinZ),
                new Vector3((float)safeBounds.MaxX, (float)safeBounds.MaxY, (float)safeBounds.MinZ),
                new Vector3((float)safeBounds.MaxX, (float)safeBounds.MinY, (float)safeBounds.MinZ),
                new Vector3((float)safeBounds.MinX, (float)safeBounds.MinY, (float)safeBounds.MinZ)),
            Material = new HelixDiffuseMaterial
            {
                DiffuseColor = color,
                EnableUnLit = true
            },
            CullMode = SharpDX.Direct3D11.CullMode.None
        };
    }

    private static HelixToolkit.SharpDX.Core.MeshGeometry3D CreateQuadGeometry(
        Vector3 topLeft,
        Vector3 topRight,
        Vector3 bottomRight,
        Vector3 bottomLeft)
    {
        return new HelixToolkit.SharpDX.Core.MeshGeometry3D
        {
            Positions =
            [
                topLeft,
                topRight,
                bottomRight,
                bottomLeft
            ],
            TextureCoordinates =
            [
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            ],
            TriangleIndices = [0, 1, 2, 0, 2, 3],
            Normals =
            [
                Vector3.UnitZ,
                Vector3.UnitZ,
                Vector3.UnitZ,
                Vector3.UnitZ
            ]
        };
    }

    private static HelixToolkit.Wpf.SharpDX.OrthographicCamera CreateTopDownCamera(SceneBounds bounds)
    {
        var safeBounds = SceneViewportRasterMath.SanitizeBounds(bounds);
        var radius = Math.Max(100, safeBounds.Radius);
        var far = Math.Max(radius * 8, 1000);
        if (double.IsNaN(far) || double.IsInfinity(far) || far <= 0.1)
        {
            far = 1000;
        }

        return new HelixToolkit.Wpf.SharpDX.OrthographicCamera
        {
            Position = new Point3D(safeBounds.CenterX, safeBounds.CenterY, Math.Max(radius * 2, safeBounds.MaxZ + radius)),
            LookDirection = new WpfVector3D(0, 0, -1),
            UpDirection = new WpfVector3D(0, 1, 0),
            Width = SceneViewportRasterMath.CalculateOrthographicWidth(safeBounds),
            NearPlaneDistance = 0.1,
            FarPlaneDistance = far
        };
    }

    private void EnsureOrigin(SceneBounds bounds)
    {
        if (_originInitialized)
        {
            return;
        }

        var safeBounds = SceneViewportRasterMath.SanitizeBounds(bounds);
        _originX = safeBounds.CenterX;
        _originY = safeBounds.CenterY;
        _originZ = safeBounds.CenterZ;
        _originInitialized = true;
        Log.Information("Scene local origin initialized at {OriginX},{OriginY},{OriginZ}", _originX, _originY, _originZ);
    }

    private Vector3 ToLocalVector(Rasteria.Core.Geometry.GeoCoordinate world, float z)
    {
        return new Vector3((float)(world.X - _originX), (float)(world.Y - _originY), z);
    }

    private SceneBounds ToLocalBounds(SceneBounds bounds)
    {
        return SceneViewportRasterMath.ToLocalBounds(bounds, _originX, _originY, _originZ);
    }

    private static TextureModel CreateCheckerTexture()
    {
        const int size = 64;
        var pixels = new byte[size * size * 4];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var white = ((x / 8) + (y / 8)) % 2 == 0;
                var offset = ((y * size) + x) * 4;
                pixels[offset] = white ? (byte)230 : (byte)40;
                pixels[offset + 1] = white ? (byte)230 : (byte)40;
                pixels[offset + 2] = white ? (byte)230 : (byte)210;
                pixels[offset + 3] = 255;
            }
        }

        return new TextureModel(pixels, Format.B8G8R8A8_UNorm, size, size);
    }

    private static bool HasHardwareGpu()
    {
        try
        {
            using var factory = new Factory1();
            foreach (var adapter in factory.Adapters1)
            {
                using (adapter)
                {
                    if ((adapter.Description1.Flags & AdapterFlags.Software) == 0)
                    {
                        return true;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "GPU adapter detection failed. Raster metadata features can continue without the 3D viewport.");
        }

        return false;
    }

    private void SetCameraForBounds(SceneBounds bounds)
    {
        _viewport.Camera = CreateTopDownCamera(bounds);
        LogCamera("TopDownOrthographic");
    }

    private void LogCamera(string action)
    {
        if (_viewport.Camera is HelixToolkit.Wpf.SharpDX.ProjectionCamera camera)
        {
            Log.Information("Camera {Action}. Position {Position}, look {LookDirection}, up {UpDirection}, near {Near}, far {Far}",
                action,
                camera.Position,
                camera.LookDirection,
                camera.UpDirection,
                camera.NearPlaneDistance,
                camera.FarPlaneDistance);
        }
    }

    private void RecalculateSceneBounds()
    {
        _sceneBounds = SceneBounds.Union(_boundsByLayer.Values);
        if (_boundsByLayer.Count == 0)
        {
            ResetOrigin();
            SetCameraForBounds(SceneBounds.Empty);
        }
    }

    private void ClearSceneResources()
    {
        foreach (var visuals in _visualsByLayer.Values)
        {
            ClearLayerVisuals(visuals);
        }

        _visualsByLayer.Clear();
        _boundsByLayer.Clear();
        _sceneBounds = SceneBounds.Empty;
        ResetOrigin();
        _viewport.Items.Clear();
        SetCameraForBounds(SceneBounds.Empty);
    }

    private int CleanupPartialVisuals(List<Element3D> visuals)
    {
        var count = visuals.Count;
        ClearLayerVisuals(visuals);
        visuals.Clear();
        Log.Information("Partial visual cleanup count {CleanupCount}", count);
        return count;
    }

    private void ClearLayerVisuals(IEnumerable<Element3D> visuals)
    {
        foreach (var visual in visuals)
        {
            RemoveAndDisposeVisual(visual);
        }
    }

    private void RemoveAndDisposeVisual(Element3D visual)
    {
        _viewport.Items.Remove(visual);

        if (visual is MeshGeometryModel3D mesh)
        {
            if (mesh.Material is HelixDiffuseMaterial diffuseMaterial)
            {
                diffuseMaterial.DiffuseMap = null;
            }

            if (mesh.Material is IDisposable materialDisposable)
            {
                materialDisposable.Dispose();
            }

            if (mesh.Geometry is IDisposable geometryDisposable)
            {
                geometryDisposable.Dispose();
            }

            mesh.Material = null!;
            mesh.Geometry = null!;
        }

        if (visual is IDisposable visualDisposable)
        {
            visualDisposable.Dispose();
        }
    }

    private void ResetOrigin()
    {
        _originInitialized = false;
        _originX = 0;
        _originY = 0;
        _originZ = 0;
    }
}
