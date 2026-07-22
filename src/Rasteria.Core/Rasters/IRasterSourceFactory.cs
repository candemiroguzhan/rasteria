namespace Rasteria.Core.Rasters;

public interface IRasterSourceFactory
{
    bool CanOpen(string filePath);
    IRasterSource Open(string filePath);
}
