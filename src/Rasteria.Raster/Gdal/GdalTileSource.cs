using Rasteria.Core.Rasters;
using OSGeo.GDAL;
using Serilog;

namespace Rasteria.Raster.Gdal;

public sealed class GdalTileSource : ITileSource
{
    public const int DefaultTileSize = 512;

    private readonly GdalRasterSource _source;
    private RasterMetadata? _metadata;

    public GdalTileSource(string filePath)
    {
        _source = new GdalRasterSource(filePath);
    }

    public Task<RasterMetadata> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        return _source.GetMetadataAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TileCoordinate>> GetVisibleTilesAsync(ViewportState viewport, CancellationToken cancellationToken = default)
    {
        var metadata = _metadata ??= await GetMetadataAsync(cancellationToken);
        return CalculateVisibleTiles(metadata, viewport);
    }

    public Task<RasterTile> GetTileAsync(TileCoordinate coordinate, CancellationToken cancellationToken = default)
    {
        return Task.Run(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var metadata = _metadata ??= await GetMetadataAsync(cancellationToken);
            var levelScale = GetLevelScale(coordinate.Level);
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

            var displayWidth = Math.Max(1, (int)Math.Ceiling(sourceWidth / (double)levelScale));
            var displayHeight = Math.Max(1, (int)Math.Ceiling(sourceHeight / (double)levelScale));

            GdalRuntime.EnsureConfigured();
            using var dataset = OSGeo.GDAL.Gdal.Open(metadata.FilePath, Access.GA_ReadOnly)
                ?? throw new InvalidOperationException($"GDAL could not open raster: {metadata.FilePath}");

            var bandCount = dataset.RasterCount;
            if (bandCount <= 0)
            {
                throw new InvalidOperationException($"Raster has no readable bands: {metadata.FilePath}");
            }

            var intSourceX = (int)sourceX;
            var intSourceY = (int)sourceY;
            var intSourceWidth = (int)sourceWidth;
            var intSourceHeight = (int)sourceHeight;
            var red = ReadBand(dataset, bandCount >= 3 ? 1 : 1, intSourceX, intSourceY, intSourceWidth, intSourceHeight, displayWidth, displayHeight);
            var green = ReadBand(dataset, bandCount >= 3 ? 2 : 1, intSourceX, intSourceY, intSourceWidth, intSourceHeight, displayWidth, displayHeight);
            var blue = ReadBand(dataset, bandCount >= 3 ? 3 : 1, intSourceX, intSourceY, intSourceWidth, intSourceHeight, displayWidth, displayHeight);
            var pixels = new byte[displayWidth * displayHeight * 4];

            for (var i = 0; i < displayWidth * displayHeight; i++)
            {
                var pixelOffset = i * 4;
                pixels[pixelOffset] = blue[i];
                pixels[pixelOffset + 1] = green[i];
                pixels[pixelOffset + 2] = red[i];
                pixels[pixelOffset + 3] = 255;
            }

            return new RasterTile(coordinate, intSourceX, intSourceY, intSourceWidth, intSourceHeight, displayWidth, displayHeight, displayWidth * 4, pixels);
        }, cancellationToken);
    }

    public static bool CanOpen(string filePath)
    {
        try
        {
            GdalRuntime.EnsureConfigured();
            using var dataset = OSGeo.GDAL.Gdal.Open(filePath, Access.GA_ReadOnly);
            return dataset is not null && dataset.RasterXSize > 0 && dataset.RasterYSize > 0;
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "GDAL is not available for {FilePath}", filePath);
            return false;
        }
    }

    public static bool IsGeoTiff(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return extension.Equals(".tif", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".geotiff", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<TileCoordinate> CalculateVisibleTiles(RasterMetadata metadata, ViewportState viewport)
    {
        if (metadata.Width <= 0 || metadata.Height <= 0 || viewport.ImageWidth <= 0 || viewport.ImageHeight <= 0)
        {
            return [];
        }

        var level = GetLevelForScale(viewport.Scale, metadata);
        var sourceTileSize = (long)metadata.TileSize * GetLevelScale(level);
        var maxTileX = Math.Max(0, (int)(((long)metadata.Width - 1) / sourceTileSize));
        var maxTileY = Math.Max(0, (int)(((long)metadata.Height - 1) / sourceTileSize));
        var minX = Math.Clamp((int)Math.Floor(viewport.ImageX / sourceTileSize), 0, maxTileX);
        var minY = Math.Clamp((int)Math.Floor(viewport.ImageY / sourceTileSize), 0, maxTileY);
        var maxX = Math.Clamp((int)Math.Floor((viewport.ImageX + viewport.ImageWidth) / sourceTileSize), 0, maxTileX);
        var maxY = Math.Clamp((int)Math.Floor((viewport.ImageY + viewport.ImageHeight) / sourceTileSize), 0, maxTileY);

        var tiles = new List<TileCoordinate>();
        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                tiles.Add(new TileCoordinate(x, y, level));
            }
        }

        return tiles;
    }

    private static byte[] ReadBand(Dataset dataset, int bandNumber, int sourceX, int sourceY, int sourceWidth, int sourceHeight, int displayWidth, int displayHeight)
    {
        using var band = dataset.GetRasterBand(bandNumber);
        band.GetNoDataValue(out var noData, out var hasNoData);

        if (band.DataType == DataType.GDT_Byte)
        {
            var byteBuffer = new byte[displayWidth * displayHeight];
            band.ReadRaster(sourceX, sourceY, sourceWidth, sourceHeight, byteBuffer, displayWidth, displayHeight, 0, 0);
            return byteBuffer;
        }

        var values = new double[displayWidth * displayHeight];
        band.ReadRaster(sourceX, sourceY, sourceWidth, sourceHeight, values, displayWidth, displayHeight, 0, 0);
        return NormalizeToByte(values, hasNoData == 1, noData);
    }

    private static byte[] NormalizeToByte(double[] values, bool hasNoData, double noData)
    {
        var valid = values.Where(value => IsDisplayValue(value, hasNoData, noData)).ToArray();
        if (valid.Length == 0)
        {
            return new byte[values.Length];
        }

        var min = valid.Min();
        var max = valid.Max();
        var output = new byte[values.Length];
        if (Math.Abs(max - min) < double.Epsilon)
        {
            Array.Fill(output, (byte)Math.Clamp(min, 0, 255));
            return output;
        }

        var range = max - min;
        for (var i = 0; i < values.Length; i++)
        {
            output[i] = IsDisplayValue(values[i], hasNoData, noData)
                ? (byte)Math.Clamp(Math.Round((values[i] - min) * 255d / range), 0, 255)
                : (byte)0;
        }

        return output;
    }

    private static bool IsDisplayValue(double value, bool hasNoData, double noData)
    {
        return !double.IsNaN(value)
               && !double.IsInfinity(value)
               && (!hasNoData || Math.Abs(value - noData) > double.Epsilon);
    }

    private static int GetLevelForScale(double scale, RasterMetadata metadata)
    {
        if (scale >= 1 || scale <= 0)
        {
            return 0;
        }

        var level = Math.Max(0, (int)Math.Floor(Math.Log(1 / scale, 2)));
        return Math.Min(level, GetMaxLevel(metadata));
    }

    private static int GetMaxLevel(RasterMetadata metadata)
    {
        var maxDimension = Math.Max(metadata.Width, metadata.Height);
        return Math.Max(0, (int)Math.Ceiling(Math.Log(Math.Max(1d, (double)maxDimension / metadata.TileSize), 2)));
    }

    private static long GetLevelScale(int level)
    {
        return 1L << Math.Clamp(level, 0, 30);
    }
}
