namespace Rasteria.Core.Geometry;

public sealed record CoordinateSystemInfo(string Name, string Authority, int? Srid, string? WellKnownText)
{
    public static CoordinateSystemInfo ImagePixels { get; } = new("Image pixels", "LOCAL", null, null);

    public string DisplayName => Srid is null ? Name : $"{Name} ({Authority}:{Srid})";
}
