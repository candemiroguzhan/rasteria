namespace Rasteria.Core.Scene;

public sealed record SceneCamera(
    SceneCameraMode Mode,
    double PositionX,
    double PositionY,
    double PositionZ,
    double LookDirectionX,
    double LookDirectionY,
    double LookDirectionZ,
    double UpDirectionX,
    double UpDirectionY,
    double UpDirectionZ,
    double FieldOfView = 45);
