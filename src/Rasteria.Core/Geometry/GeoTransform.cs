namespace Rasteria.Core.Geometry;

public sealed record GeoTransform(double OriginX, double PixelWidth, double RotationX, double OriginY, double RotationY, double PixelHeight)
{
    public static GeoTransform Identity { get; } = new(0, 1, 0, 0, 0, 1);

    public GeoCoordinate PixelToWorld(double pixelX, double pixelY)
    {
        var x = OriginX + pixelX * PixelWidth + pixelY * RotationX;
        var y = OriginY + pixelX * RotationY + pixelY * PixelHeight;
        return new GeoCoordinate(x, y);
    }

    public GeoCoordinate WorldToPixel(double worldX, double worldY)
    {
        var determinant = PixelWidth * PixelHeight - RotationX * RotationY;
        if (Math.Abs(determinant) < double.Epsilon)
        {
            return new GeoCoordinate(worldX, worldY);
        }

        var dx = worldX - OriginX;
        var dy = worldY - OriginY;
        return new GeoCoordinate(
            (PixelHeight * dx - RotationX * dy) / determinant,
            (-RotationY * dx + PixelWidth * dy) / determinant);
    }
}
