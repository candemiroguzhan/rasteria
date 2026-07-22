using Rasteria.Core.Rasters;

namespace Rasteria.Raster.Gdal;

public sealed class GdalRasterSourceFactory : IRasterSourceFactory, ITileSourceFactory
{
    public bool CanOpen(string filePath)
    {
        return GdalTileSource.CanOpen(filePath);
    }

    public IRasterSource Open(string filePath)
    {
        if (!CanOpen(filePath))
        {
            throw new InvalidOperationException($"GDAL could not open raster: {filePath}");
        }

        return new GdalRasterSource(filePath);
    }

    public ITileSource Create(string filePath)
    {
        if (!CanOpen(filePath))
        {
            throw new InvalidOperationException($"GDAL could not open raster for tile rendering: {filePath}");
        }

        return new GdalTileSource(filePath);
    }
}
