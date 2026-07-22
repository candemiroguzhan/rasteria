using Rasteria.Core.Geometry;
using Rasteria.Core.Scene;

namespace Rasteria.Core.Layers;

public interface IGeoLayer
{
    Guid Id { get; }
    string Name { get; }
    string SourcePath { get; }
    GeoLayerType Type { get; }
    bool IsVisible { get; }
    double Opacity { get; }
    SceneBounds Bounds { get; }
    CoordinateSystemInfo CoordinateSystem { get; }
    IReadOnlyDictionary<string, string> Metadata { get; }
}
