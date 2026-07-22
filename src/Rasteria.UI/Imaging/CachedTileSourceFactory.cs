using Rasteria.Core.Rasters;

namespace Rasteria.UI.Imaging;

public sealed class CachedTileSourceFactory
{
    private readonly ITileSourceFactory _inner;
    private readonly ITileCacheService _cache;

    public CachedTileSourceFactory(ITileSourceFactory inner, ITileCacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public ITileSource Create(string filePath)
    {
        return new CachedTileSource(_inner.Create(filePath), _cache, filePath);
    }
}
