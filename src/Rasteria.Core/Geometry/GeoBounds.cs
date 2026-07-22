namespace Rasteria.Core.Geometry;

public readonly record struct GeoBounds(double MinX, double MinY, double MaxX, double MaxY)
{
    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;

    public static GeoBounds Empty => new(0, 0, 0, 0);
}
