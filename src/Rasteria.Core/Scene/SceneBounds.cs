namespace Rasteria.Core.Scene;

public readonly record struct SceneBounds(double MinX, double MinY, double MinZ, double MaxX, double MaxY, double MaxZ)
{
    public double Width => MaxX - MinX;
    public double Depth => MaxY - MinY;
    public double Height => MaxZ - MinZ;
    public double CenterX => (MinX + MaxX) / 2;
    public double CenterY => (MinY + MaxY) / 2;
    public double CenterZ => (MinZ + MaxZ) / 2;
    public double Radius => Math.Sqrt((Width * Width) + (Depth * Depth) + (Height * Height)) / 2;

    public static SceneBounds Empty => new(0, 0, 0, 0, 0, 0);

    public static SceneBounds Union(IEnumerable<SceneBounds> bounds)
    {
        var items = bounds.Where(b => b.Width != 0 || b.Depth != 0 || b.Height != 0).ToArray();
        return items.Length == 0
            ? Empty
            : new SceneBounds(
                items.Min(b => b.MinX),
                items.Min(b => b.MinY),
                items.Min(b => b.MinZ),
                items.Max(b => b.MaxX),
                items.Max(b => b.MaxY),
                items.Max(b => b.MaxZ));
    }
}
