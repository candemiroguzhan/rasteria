using Rasteria.Core.Rasters;

namespace Rasteria.UI.Imaging;

public interface ITilePathProvider
{
    string GetTilePath(TileCoordinate coordinate);
}
