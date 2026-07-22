using Rasteria.Core.Layers;
using Rasteria.Core.Rasters;
using Rasteria.Core.Scene;

namespace Rasteria.Core.Projects;

public sealed class ViewerSession
{
    private readonly List<LayerState> _layers = [];

    public IReadOnlyList<LayerState> Layers => _layers;
    public LayerState? ActiveLayer { get; private set; }
    public RasterMetadata? ActiveRasterMetadata { get; private set; }
    public SceneModel Scene { get; } = new();

    public LayerState AddRaster(string filePath, RasterMetadata metadata)
    {
        var layer = new LayerState(Guid.NewGuid(), Path.GetFileName(filePath), filePath, LayerKind.Raster);
        _layers.Add(layer);
        ActiveLayer = layer;
        ActiveRasterMetadata = metadata;
        return layer;
    }

    public SceneLayer AddRasterSceneLayer(string filePath, RasterMetadata metadata, ITileSource tileSource, bool isDem = false)
    {
        var layer = AddRaster(filePath, metadata);
        if (isDem)
        {
            var demLayer = new DemLayer(layer.Id, layer.Name, filePath, metadata, ToSceneBounds(metadata));
            var terrainSceneLayer = new TerrainSceneLayer(demLayer, tileSource);
            Scene.AddLayer(terrainSceneLayer);
            return terrainSceneLayer;
        }

        var rasterLayer = new RasterLayer(layer.Id, layer.Name, filePath, metadata, ToSceneBounds(metadata), isDem);
        var sceneLayer = new RasterSceneLayer(rasterLayer, tileSource);
        Scene.AddLayer(sceneLayer);
        return sceneLayer;
    }

    public LayerState AddMeshPlaceholder(string filePath)
    {
        var layer = new LayerState(Guid.NewGuid(), Path.GetFileName(filePath), filePath, LayerKind.Mesh);
        _layers.Add(layer);
        ActiveLayer = layer;
        return layer;
    }


    private static SceneBounds ToSceneBounds(RasterMetadata metadata)
    {
        return new SceneBounds(metadata.Bounds.MinX, metadata.Bounds.MinY, 0, metadata.Bounds.MaxX, metadata.Bounds.MaxY, 0);
    }

}
