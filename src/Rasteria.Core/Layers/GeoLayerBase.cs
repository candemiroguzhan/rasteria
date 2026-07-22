using Rasteria.Core.Geometry;
using Rasteria.Core.Scene;

namespace Rasteria.Core.Layers;

public abstract record GeoLayerBase(
    Guid Id,
    string Name,
    string SourcePath,
    GeoLayerType Type,
    SceneBounds Bounds,
    CoordinateSystemInfo CoordinateSystem,
    IReadOnlyDictionary<string, string> Metadata,
    bool IsVisible = true,
    double Opacity = 1) : IGeoLayer;
