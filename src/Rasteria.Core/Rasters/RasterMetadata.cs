using Rasteria.Core.Geometry;

namespace Rasteria.Core.Rasters;

public sealed record RasterMetadata(
    string FilePath,
    int Width,
    int Height,
    int BandCount,
    int TileSize,
    bool IsGeoTiff,
    bool IsCloudOptimizedGeoTiffCandidate,
    string ProviderName,
    CoordinateSystemInfo CoordinateSystem,
    GeoTransform GeoTransform,
    GeoBounds Bounds,
    double ResolutionX,
    double ResolutionY,
    IReadOnlyList<RasterBandInfo> Bands,
    IReadOnlyDictionary<string, string> Metadata)
{
    public double? NoDataValue => Bands.FirstOrDefault(b => b.NoDataValue.HasValue)?.NoDataValue;
}
