using Rasteria.Core.Rasters;
using Rasteria.Core.Scene;

namespace Rasteria.Core.Layers;

public sealed record DemLayer(
    Guid Id,
    string Name,
    string SourcePath,
    RasterMetadata RasterMetadata,
    SceneBounds Bounds,
    bool IsVisible = true,
    double Opacity = 1)
    : GeoLayerBase(
        Id,
        Name,
        SourcePath,
        GeoLayerType.Dem,
        Bounds,
        RasterMetadata.CoordinateSystem,
        RasterMetadata.Metadata,
        IsVisible,
        Opacity);
