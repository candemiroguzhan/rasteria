using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rasteria.Core.Rasters;

namespace Rasteria.UI.Imaging;

public sealed class CachedTileSource : ITileSource, ITilePathProvider
{
    private readonly ITileSource _inner;
    private readonly ITileCacheService _cache;
    private readonly string _sourceHash;

    public CachedTileSource(ITileSource inner, ITileCacheService cache, string filePath)
    {
        _inner = inner;
        _cache = cache;
        _sourceHash = CreateSourceHash(filePath);
    }

    public Task<RasterMetadata> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        return _inner.GetMetadataAsync(cancellationToken);
    }

    public Task<IReadOnlyList<TileCoordinate>> GetVisibleTilesAsync(ViewportState viewport, CancellationToken cancellationToken = default)
    {
        return _inner.GetVisibleTilesAsync(viewport, cancellationToken);
    }

    public async Task<RasterTile> GetTileAsync(TileCoordinate coordinate, CancellationToken cancellationToken = default)
    {
        var key = CreateKey(coordinate);
        var cached = _cache.TryGetMemory(key) ?? await _cache.TryGetDiskAsync(key, cancellationToken);
        if (cached is not null)
        {
            var metadata = await _inner.GetMetadataAsync(cancellationToken);
            return ToRasterTile(coordinate, cached, metadata);
        }

        var tile = await _inner.GetTileAsync(coordinate, cancellationToken);
        await _cache.SaveAsync(key, ToBitmapSource(tile), cancellationToken);
        return tile;
    }

    public string GetTilePath(TileCoordinate coordinate)
    {
        return _cache.GetTilePath(CreateKey(coordinate));
    }

    private TileKey CreateKey(TileCoordinate coordinate)
    {
        return new TileKey(_sourceHash, coordinate.Level, coordinate.X, coordinate.Y);
    }

    private static BitmapSource ToBitmapSource(RasterTile tile)
    {
        var bitmap = BitmapSource.Create(tile.PixelWidth, tile.PixelHeight, 96, 96, PixelFormats.Bgra32, null, tile.Bgra32Pixels, tile.Stride);
        bitmap.Freeze();
        return bitmap;
    }

    private static RasterTile ToRasterTile(TileCoordinate coordinate, BitmapSource bitmap, RasterMetadata metadata)
    {
        var source = bitmap.Format == PixelFormats.Bgra32 ? bitmap : new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        if (source.CanFreeze && !source.IsFrozen)
        {
            source.Freeze();
        }

        var stride = source.PixelWidth * 4;
        var pixels = new byte[stride * source.PixelHeight];
        source.CopyPixels(pixels, stride, 0);
        var levelScale = 1L << Math.Clamp(coordinate.Level, 0, 30);
        var sourceTileSize = (long)metadata.TileSize * levelScale;
        var sourceX = (long)coordinate.X * sourceTileSize;
        var sourceY = (long)coordinate.Y * sourceTileSize;
        if (sourceX < 0 || sourceY < 0 || sourceX >= metadata.Width || sourceY >= metadata.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate), coordinate, "Tile coordinate is outside the raster bounds.");
        }

        var sourceWidth = Math.Min(sourceTileSize, (long)metadata.Width - sourceX);
        var sourceHeight = Math.Min(sourceTileSize, (long)metadata.Height - sourceY);
        if (sourceWidth <= 0 || sourceHeight <= 0 || sourceWidth > int.MaxValue || sourceHeight > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate), coordinate, "Tile coordinate is outside the raster bounds.");
        }

        return new RasterTile(coordinate, (int)sourceX, (int)sourceY, (int)sourceWidth, (int)sourceHeight, source.PixelWidth, source.PixelHeight, stride, pixels);
    }

    private static string CreateSourceHash(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        var info = new FileInfo(fullPath);
        var input = $"{fullPath}|{info.LastWriteTimeUtc.Ticks}|{info.Length}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)))[..16].ToLowerInvariant();
    }
}
