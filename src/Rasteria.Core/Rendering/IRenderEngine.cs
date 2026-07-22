using Rasteria.Core.Layers;
using Rasteria.Core.Rasters;

namespace Rasteria.Core.Rendering;

public interface IRenderEngine
{
    string Name { get; }
    Task RenderAsync(IReadOnlyList<LayerState> layers, ViewportState viewport, CancellationToken cancellationToken = default);
}
