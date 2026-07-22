using Rasteria.Core.Layers;

namespace Rasteria.Core.Scene;

public abstract record SceneLayer(IGeoLayer GeoLayer)
{
    public Guid Id => GeoLayer.Id;
    public string Name => GeoLayer.Name;
    public GeoLayerType Type => GeoLayer.Type;
    public SceneBounds Bounds => GeoLayer.Bounds;
}
