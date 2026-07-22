namespace Rasteria.Core.Scene;

public sealed class SceneModel
{
    private readonly List<SceneLayer> _layers = [];

    public IReadOnlyList<SceneLayer> Layers => _layers;
    public SceneBounds Bounds => SceneBounds.Union(_layers.Select(layer => layer.Bounds));
    public SceneLayer? ActiveLayer { get; private set; }

    public void AddLayer(SceneLayer layer)
    {
        _layers.Add(layer);
        ActiveLayer = layer;
    }

    public void Clear()
    {
        _layers.Clear();
        ActiveLayer = null;
    }
}
