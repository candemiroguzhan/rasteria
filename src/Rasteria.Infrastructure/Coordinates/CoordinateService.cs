using Rasteria.Core.Geometry;
using Rasteria.Core.Rasters;

namespace Rasteria.Infrastructure.Coordinates;

public sealed class CoordinateService
{
    public GeoCoordinate PixelToWorld(RasterMetadata metadata, GeoCoordinate pixel)
    {
        return metadata.GeoTransform.PixelToWorld(pixel.X, pixel.Y);
    }

    public GeoCoordinate WorldToPixel(RasterMetadata metadata, GeoCoordinate world)
    {
        return metadata.GeoTransform.WorldToPixel(world.X, world.Y);
    }
}
