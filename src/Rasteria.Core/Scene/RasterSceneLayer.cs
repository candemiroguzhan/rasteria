using Rasteria.Core.Layers;
using Rasteria.Core.Rasters;

namespace Rasteria.Core.Scene;

public sealed record RasterSceneLayer(RasterLayer RasterLayer, ITileSource TileSource) : SceneLayer(RasterLayer);
