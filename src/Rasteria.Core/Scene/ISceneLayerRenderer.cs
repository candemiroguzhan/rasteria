namespace Rasteria.Core.Scene;

public interface ISceneLayerRenderer
{
    bool CanRender(SceneLayer layer);
    Task AddLayerAsync(SceneLayer layer, CancellationToken cancellationToken = default);
    void RemoveLayer(Guid layerId);
    void SetLayerVisibility(Guid layerId, bool isVisible);
    void SetLayerOpacity(Guid layerId, double opacity);
}
