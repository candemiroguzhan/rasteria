using System.Windows.Media.Imaging;
using System.IO;

namespace Rasteria.UI.Imaging;

public sealed class TempTileCacheService : ITileCacheService
{
    private readonly Dictionary<TileKey, BitmapSource> _memory = [];
    private readonly string _root = Path.Combine(Path.GetTempPath(), "Rasteria", "tiles");

    public BitmapSource? TryGetMemory(TileKey key)
    {
        return _memory.TryGetValue(key, out var bitmap) ? bitmap : null;
    }

    public Task<BitmapSource?> TryGetDiskAsync(TileKey key, CancellationToken cancellationToken = default)
    {
        var path = GetTilePath(key);
        if (!File.Exists(path))
        {
            return Task.FromResult<BitmapSource?>(null);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        _memory[key] = bitmap;
        return Task.FromResult<BitmapSource?>(bitmap);
    }

    public Task SaveAsync(TileKey key, BitmapSource bitmap, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(Path.GetDirectoryName(GetTilePath(key))!);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(GetTilePath(key));
        encoder.Save(stream);
        _memory[key] = bitmap;
        return Task.CompletedTask;
    }

    public string GetTilePath(TileKey key)
    {
        return Path.Combine(_root, key.SourceHash, key.Level.ToString(), $"{key.X}_{key.Y}.png");
    }

    public void ClearMemory()
    {
        _memory.Clear();
    }
}
