using System.Windows.Media.Imaging;

namespace Rasteria.UI.Imaging;

public interface ITileCacheService
{
    BitmapSource? TryGetMemory(TileKey key);
    Task<BitmapSource?> TryGetDiskAsync(TileKey key, CancellationToken cancellationToken = default);
    Task SaveAsync(TileKey key, BitmapSource bitmap, CancellationToken cancellationToken = default);
    string GetTilePath(TileKey key);
    void ClearMemory();
}
