using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rasteria.Core.Geometry;
using Rasteria.Core.Rasters;
using Rasteria.UI.ViewModels;

namespace Rasteria.UI.Controls;

public partial class MapViewer : UserControl
{
    public static readonly DependencyProperty RasterLayersProperty = DependencyProperty.Register(
        nameof(RasterLayers),
        typeof(IEnumerable),
        typeof(MapViewer),
        new FrameworkPropertyMetadata(null, OnRasterLayersChanged));

    public static readonly DependencyProperty SelectedRasterMetadataProperty = DependencyProperty.Register(
        nameof(SelectedRasterMetadata),
        typeof(RasterMetadata),
        typeof(MapViewer),
        new FrameworkPropertyMetadata(null, OnSelectedRasterMetadataChanged));

    public static readonly DependencyProperty ZoomLevelProperty = DependencyProperty.Register(
        nameof(ZoomLevel),
        typeof(double),
        typeof(MapViewer),
        new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MouseImagePositionProperty = DependencyProperty.Register(
        nameof(MouseImagePosition),
        typeof(GeoCoordinate),
        typeof(MapViewer),
        new FrameworkPropertyMetadata(new GeoCoordinate(0, 0), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    private readonly Dictionary<LayerTileKey, RenderedTile> _tiles = [];
    private readonly HashSet<LayerTileKey> _pendingTiles = [];
    private readonly List<LayerViewModel> _subscribedLayers = [];
    private readonly object _tileGate = new();
    private INotifyCollectionChanged? _layerCollection;
    private CancellationTokenSource? _tileLoadCts;
    private double _scale = 1;
    private Vector _offset = new(0, 0);
    private Point? _lastPanPoint;

    public MapViewer()
    {
        InitializeComponent();
        ClipToBounds = true;
        Loaded += (_, _) => FitToFullExtent();
        SizeChanged += (_, _) =>
        {
            QueueVisibleTiles();
            InvalidateVisual();
        };
        Unloaded += (_, _) =>
        {
            CancelTileLoading();
            DetachLayerCollection();
            DetachLayerHandlers(_subscribedLayers);
        };
    }

    public IEnumerable? RasterLayers
    {
        get => (IEnumerable?)GetValue(RasterLayersProperty);
        set => SetValue(RasterLayersProperty, value);
    }

    public RasterMetadata? SelectedRasterMetadata
    {
        get => (RasterMetadata?)GetValue(SelectedRasterMetadataProperty);
        set => SetValue(SelectedRasterMetadataProperty, value);
    }

    public double ZoomLevel
    {
        get => (double)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    public GeoCoordinate MouseImagePosition
    {
        get => (GeoCoordinate)GetValue(MouseImagePositionProperty);
        set => SetValue(MouseImagePositionProperty, value);
    }

    public void FitToFullExtent()
    {
        var metadata = SelectedRasterMetadata ?? GetLayers().LastOrDefault(l => l.Metadata is not null)?.Metadata;
        if (metadata is null || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        _scale = Math.Min(ActualWidth / metadata.Width, ActualHeight / metadata.Height);
        _offset = new Vector((ActualWidth - metadata.Width * _scale) / 2, (ActualHeight - metadata.Height * _scale) / 2);
        ZoomLevel = _scale;
        QueueVisibleTiles();
        InvalidateVisual();
    }

    public void ZoomBy(double factor)
    {
        ZoomAt(new Point(ActualWidth / 2, ActualHeight / 2), factor);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
        drawingContext.DrawRectangle(new SolidColorBrush(Color.FromRgb(16, 19, 24)), null, new Rect(0, 0, ActualWidth, ActualHeight));

        var layers = GetLayers();
        if (layers.Count == 0)
        {
            DrawCenteredText(drawingContext, "Open a raster to begin");
            drawingContext.Pop();
            return;
        }

        Dictionary<LayerTileKey, RenderedTile> tiles;
        lock (_tileGate)
        {
            tiles = new Dictionary<LayerTileKey, RenderedTile>(_tiles);
        }

        if (tiles.Count == 0 && layers.Any(l => l.IsVisible && l.Opacity > 0))
        {
            DrawCenteredText(drawingContext, "Loading tiles...");
            drawingContext.Pop();
            return;
        }

        foreach (var layer in layers)
        {
            if (!layer.IsVisible || layer.Opacity <= 0 || layer.Metadata is null)
            {
                continue;
            }

            drawingContext.PushOpacity(Math.Clamp(layer.Opacity, 0, 1));
            var viewport = GetViewportState(layer.Metadata);
            foreach (var tile in tiles.Values.Where(t => t.LayerId == layer.Id))
            {
                if (!IntersectsViewport(tile.Tile, viewport))
                {
                    continue;
                }

                var rect = RasterToScreen(tile.Tile.SourceX, tile.Tile.SourceY, tile.Tile.SourceWidth, tile.Tile.SourceHeight);
                if (rect.Width > 0 && rect.Height > 0)
                {
                    drawingContext.DrawImage(tile.Bitmap, rect);
                }
            }

            drawingContext.Pop();
        }

        drawingContext.Pop();
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        ZoomAt(e.GetPosition(this), e.Delta > 0 ? 1.15 : 1 / 1.15);
        e.Handled = true;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (GetLayers().Count == 0)
        {
            return;
        }

        Focus();
        _lastPanPoint = e.GetPosition(this);
        CaptureMouse();
        Cursor = Cursors.SizeAll;
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var metadata = SelectedRasterMetadata;
        if (metadata is null)
        {
            return;
        }

        var point = e.GetPosition(this);
        var imagePoint = ToImage(point);
        if (imagePoint.X >= 0 && imagePoint.Y >= 0 && imagePoint.X < metadata.Width && imagePoint.Y < metadata.Height)
        {
            MouseImagePosition = new GeoCoordinate(Math.Floor(imagePoint.X), Math.Floor(imagePoint.Y));
        }

        if (_lastPanPoint is not null && e.LeftButton == MouseButtonState.Pressed)
        {
            _offset += point - _lastPanPoint.Value;
            _lastPanPoint = point;
            QueueVisibleTiles();
            InvalidateVisual();
        }

        e.Handled = true;
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        _lastPanPoint = null;
        ReleaseMouseCapture();
        Cursor = Cursors.Arrow;
        e.Handled = true;
    }

    private static void OnRasterLayersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewer = (MapViewer)d;
        viewer.DetachLayerCollection();
        viewer.AttachLayerCollection(e.NewValue as IEnumerable);
        viewer.ResetTiles();
        viewer.QueueVisibleTiles();
        viewer.InvalidateVisual();
    }

    private static void OnSelectedRasterMetadataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewer = (MapViewer)d;
        viewer.FitToFullExtent();
    }

    private void AttachLayerCollection(IEnumerable? layers)
    {
        if (layers is INotifyCollectionChanged collection)
        {
            _layerCollection = collection;
            collection.CollectionChanged += RasterLayers_CollectionChanged;
        }

        AttachLayerHandlers(GetLayers());
    }

    private void DetachLayerCollection()
    {
        if (_layerCollection is not null)
        {
            _layerCollection.CollectionChanged -= RasterLayers_CollectionChanged;
            _layerCollection = null;
        }
    }

    private void RasterLayers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            DetachLayerHandlers(e.OldItems.OfType<LayerViewModel>());
            RemoveTilesForLayers(e.OldItems.OfType<LayerViewModel>().Select(l => l.Id));
        }

        if (e.NewItems is not null)
        {
            AttachLayerHandlers(e.NewItems.OfType<LayerViewModel>());
        }

        QueueVisibleTiles();
        InvalidateVisual();
    }

    private void AttachLayerHandlers(IEnumerable<LayerViewModel> layers)
    {
        foreach (var layer in layers)
        {
            if (_subscribedLayers.Contains(layer))
            {
                continue;
            }

            layer.PropertyChanged += Layer_PropertyChanged;
            _subscribedLayers.Add(layer);
        }
    }

    private void DetachLayerHandlers(IEnumerable<LayerViewModel> layers)
    {
        foreach (var layer in layers.ToArray())
        {
            layer.PropertyChanged -= Layer_PropertyChanged;
            _subscribedLayers.Remove(layer);
        }
    }

    private void Layer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LayerViewModel.Opacity) or nameof(LayerViewModel.IsVisible))
        {
            InvalidateVisual();
        }
    }

    private void ZoomAt(Point center, double factor)
    {
        var metadata = SelectedRasterMetadata ?? GetLayers().LastOrDefault(l => l.Metadata is not null)?.Metadata;
        if (metadata is null)
        {
            return;
        }

        var imagePoint = ToImage(center);
        _scale = Math.Clamp(_scale * factor, 0.01, 80);
        _offset = new Vector(center.X - imagePoint.X * _scale, center.Y - imagePoint.Y * _scale);
        ZoomLevel = _scale;
        QueueVisibleTiles();
        InvalidateVisual();
    }

    private void QueueVisibleTiles()
    {
        var layers = GetLayers().Where(l => l.TileSource is not null && l.Metadata is not null).ToArray();
        if (layers.Length == 0 || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        _ = LoadVisibleTilesAsync(layers, ResetTileLoading().Token);
    }

    private async Task LoadVisibleTilesAsync(IReadOnlyList<LayerViewModel> layers, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var layer in layers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (layer.TileSource is null || layer.Metadata is null)
                {
                    continue;
                }

                var visibleTiles = await layer.TileSource.GetVisibleTilesAsync(GetViewportState(layer.Metadata), cancellationToken);
                foreach (var coordinate in visibleTiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var key = new LayerTileKey(layer.Id, coordinate);
                    if (HasTileOrPending(key))
                    {
                        continue;
                    }

                    MarkPending(key);
                    _ = LoadTileAsync(layer.Id, layer.TileSource, coordinate, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task LoadTileAsync(Guid layerId, ITileSource tileSource, TileCoordinate coordinate, CancellationToken cancellationToken)
    {
        var key = new LayerTileKey(layerId, coordinate);
        try
        {
            var rasterTile = await tileSource.GetTileAsync(coordinate, cancellationToken);
            var bitmap = BitmapSource.Create(rasterTile.PixelWidth, rasterTile.PixelHeight, 96, 96, PixelFormats.Bgra32, null, rasterTile.Bgra32Pixels, rasterTile.Stride);
            bitmap.Freeze();
            await Dispatcher.InvokeAsync(() =>
            {
                lock (_tileGate)
                {
                    _tiles[key] = new RenderedTile(layerId, rasterTile, bitmap);
                    _pendingTiles.Remove(key);
                }

                InvalidateVisual();
            });
        }
        catch
        {
            ClearPending(key);
        }
    }

    private bool HasTileOrPending(LayerTileKey key)
    {
        lock (_tileGate)
        {
            return _tiles.ContainsKey(key) || _pendingTiles.Contains(key);
        }
    }

    private void MarkPending(LayerTileKey key)
    {
        lock (_tileGate)
        {
            _pendingTiles.Add(key);
        }
    }

    private void ClearPending(LayerTileKey key)
    {
        lock (_tileGate)
        {
            _pendingTiles.Remove(key);
        }
    }

    private void RemoveTilesForLayers(IEnumerable<Guid> layerIds)
    {
        var ids = layerIds.ToHashSet();
        lock (_tileGate)
        {
            foreach (var key in _tiles.Keys.Where(k => ids.Contains(k.LayerId)).ToArray())
            {
                _tiles.Remove(key);
            }

            _pendingTiles.RemoveWhere(k => ids.Contains(k.LayerId));
        }
    }

    private CancellationTokenSource ResetTileLoading()
    {
        CancelTileLoading();
        _tileLoadCts = new CancellationTokenSource();
        return _tileLoadCts;
    }

    private void CancelTileLoading()
    {
        _tileLoadCts?.Cancel();
        _tileLoadCts?.Dispose();
        _tileLoadCts = null;
    }

    private void ResetTiles()
    {
        CancelTileLoading();
        lock (_tileGate)
        {
            _tiles.Clear();
            _pendingTiles.Clear();
        }
    }

    private ViewportState GetViewportState(RasterMetadata metadata)
    {
        var topLeft = ToImage(new Point(0, 0));
        return new ViewportState(topLeft.X, topLeft.Y, ActualWidth / _scale, ActualHeight / _scale, _scale, (int)ActualWidth, (int)ActualHeight);
    }

    private Rect RasterToScreen(double x, double y, double width, double height)
    {
        return new Rect(_offset.X + x * _scale, _offset.Y + y * _scale, width * _scale, height * _scale);
    }

    private Point ToImage(Point point)
    {
        return new Point((point.X - _offset.X) / _scale, (point.Y - _offset.Y) / _scale);
    }

    private List<LayerViewModel> GetLayers()
    {
        return RasterLayers?.OfType<LayerViewModel>().ToList() ?? [];
    }

    private static bool IntersectsViewport(RasterTile tile, ViewportState viewport)
    {
        return tile.SourceX + tile.SourceWidth >= viewport.ImageX
               && tile.SourceY + tile.SourceHeight >= viewport.ImageY
               && tile.SourceX <= viewport.ImageX + viewport.ImageWidth
               && tile.SourceY <= viewport.ImageY + viewport.ImageHeight;
    }

    private void DrawCenteredText(DrawingContext dc, string text)
    {
        var formatted = new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 16, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(formatted, new Point((ActualWidth - formatted.Width) / 2, (ActualHeight - formatted.Height) / 2));
    }

    private readonly record struct LayerTileKey(Guid LayerId, TileCoordinate Coordinate);

    private sealed record RenderedTile(Guid LayerId, RasterTile Tile, BitmapSource Bitmap);
}
