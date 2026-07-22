using Rasteria.Core.Rasters;
using Rasteria.Core.Scene;

namespace Rasteria.Core.Layers;

public sealed record RasterLayer(
    Guid Id,
    string Name,
    string SourcePath,
    RasterMetadata RasterMetadata,
    SceneBounds Bounds,
    bool IsDem,
    bool IsVisible = true,
    double Opacity = 1)
    : GeoLayerBase(
        Id,
        Name,
        SourcePath,
        IsDem ? GeoLayerType.Dem : GeoLayerType.Raster,
        Bounds,
        RasterMetadata.CoordinateSystem,
        RasterMetadata.Metadata,
        IsVisible,
        Opacity);
