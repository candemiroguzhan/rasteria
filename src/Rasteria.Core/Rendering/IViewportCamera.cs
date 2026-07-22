using Rasteria.Core.Geometry;

namespace Rasteria.Core.Rendering;

public interface IViewportCamera
{
    double Scale { get; }
    GeoCoordinate Offset { get; }
    void Pan(double deltaX, double deltaY);
    void ZoomAt(double imageX, double imageY, double factor);
    void ZoomToFullExtent(double contentWidth, double contentHeight, double viewportWidth, double viewportHeight);
}
