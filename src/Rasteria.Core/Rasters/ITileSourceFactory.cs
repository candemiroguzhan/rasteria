namespace Rasteria.Core.Rasters;

public interface ITileSourceFactory
{
    ITileSource Create(string filePath);
}
