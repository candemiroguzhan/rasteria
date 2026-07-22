using Rasteria.Core.Layers;
using Rasteria.Core.Rasters;

namespace Rasteria.Core.Scene;

public sealed record TerrainSceneLayer(DemLayer DemLayer, ITileSource TileSource) : SceneLayer(DemLayer);
