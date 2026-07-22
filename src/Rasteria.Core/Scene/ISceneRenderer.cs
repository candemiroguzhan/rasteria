namespace Rasteria.Core.Scene;

public interface ISceneRenderer
{
    Task AddLayerAsync(SceneLayer layer, CancellationToken cancellationToken = default);
    Task SetSceneAsync(SceneModel scene, CancellationToken cancellationToken = default);
    void RemoveLayer(Guid layerId);
    void SetLayerVisibility(Guid layerId, bool isVisible);
    void SetLayerOpacity(Guid layerId, double opacity);
    void FitLayer(Guid layerId);
    void FitScene();
    void SetCameraMode(SceneCameraMode mode);
}
