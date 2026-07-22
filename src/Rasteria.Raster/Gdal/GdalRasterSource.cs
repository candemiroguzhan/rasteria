using Rasteria.Core.Geometry;
using Rasteria.Core.Rasters;
using OSGeo.GDAL;
using Serilog;

namespace Rasteria.Raster.Gdal;

public sealed class GdalRasterSource : IRasterSource
{
    private readonly string _filePath;
    private RasterMetadata? _metadata;

    public GdalRasterSource(string filePath)
    {
        _filePath = filePath;
    }

    public string FilePath => _filePath;

    public Task<RasterMetadata> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_metadata is not null)
            {
                return _metadata;
            }

            GdalRuntime.EnsureConfigured();
            using var dataset = OpenDataset();
            var geoTransform = ReadGeoTransform(dataset);
            var coordinateSystem = ReadCoordinateSystem(dataset);
            var bands = ReadBands(dataset);
            var metadata = ReadMetadata(dataset);
            var bounds = CalculateBounds(dataset.RasterXSize, dataset.RasterYSize, geoTransform);

            _metadata = new RasterMetadata(
                _filePath,
                dataset.RasterXSize,
                dataset.RasterYSize,
                dataset.RasterCount,
                GdalTileSource.DefaultTileSize,
                GdalTileSource.IsGeoTiff(_filePath),
                IsCogCandidate(dataset, metadata),
                nameof(GdalRasterSource),
                coordinateSystem,
                geoTransform,
                bounds,
                Math.Abs(geoTransform.PixelWidth),
                Math.Abs(geoTransform.PixelHeight),
                bands,
                metadata);

            return _metadata;
        }, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private Dataset OpenDataset()
    {
        return OSGeo.GDAL.Gdal.Open(_filePath, Access.GA_ReadOnly)
               ?? throw new InvalidOperationException($"GDAL could not open raster: {_filePath}");
    }

    internal static GeoTransform ReadGeoTransform(Dataset dataset)
    {
        var transform = new double[6];
        try
        {
            dataset.GetGeoTransform(transform);
            return new GeoTransform(transform[0], transform[1], transform[2], transform[3], transform[4], transform[5]);
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "GDAL could not read geotransform; falling back to identity transform");
            return GeoTransform.Identity;
        }
    }

    internal static CoordinateSystemInfo ReadCoordinateSystem(Dataset dataset)
    {
        var projection = dataset.GetProjectionRef();
        if (string.IsNullOrWhiteSpace(projection))
        {
            projection = dataset.GetProjection();
        }

        if (string.IsNullOrWhiteSpace(projection))
        {
            return CoordinateSystemInfo.ImagePixels;
        }

        var authority = "WKT";
        int? srid = null;
        var name = "Projected/Geographic CRS";

        try
        {
            using var spatialReference = new OSGeo.OSR.SpatialReference(projection);
            spatialReference.AutoIdentifyEPSG();
            name = spatialReference.GetAttrValue("PROJCS", 0)
                   ?? spatialReference.GetAttrValue("GEOGCS", 0)
                   ?? name;
            authority = spatialReference.GetAuthorityName(null) ?? authority;
            if (int.TryParse(spatialReference.GetAuthorityCode(null), out var parsedSrid))
            {
                srid = parsedSrid;
            }
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "GDAL loaded CRS WKT but OSR could not parse authority metadata");
        }

        return new CoordinateSystemInfo(name, authority, srid, projection);
    }

    internal static IReadOnlyList<RasterBandInfo> ReadBands(Dataset dataset)
    {
        var bands = new List<RasterBandInfo>();
        for (var i = 1; i <= dataset.RasterCount; i++)
        {
            using var band = dataset.GetRasterBand(i);
            band.GetNoDataValue(out var noData, out var hasNoData);
            bands.Add(new RasterBandInfo(
                i,
                band.DataType.ToString(),
                band.GetRasterColorInterpretation().ToString(),
                hasNoData == 1 ? noData : null));
        }

        return bands;
    }

    internal static IReadOnlyDictionary<string, string> ReadMetadata(Dataset dataset)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var domain in new[] { "", "IMAGE_STRUCTURE", "GEOLOCATION", "RPC" })
        {
            foreach (var item in dataset.GetMetadata(domain) ?? [])
            {
                var split = item.IndexOf('=');
                if (split > 0)
                {
                    var key = string.IsNullOrWhiteSpace(domain) ? item[..split] : $"{domain}:{item[..split]}";
                    result[key] = item[(split + 1)..];
                }
            }
        }

        return result;
    }

    private static GeoBounds CalculateBounds(int width, int height, GeoTransform transform)
    {
        var points = new[]
        {
            transform.PixelToWorld(0, 0),
            transform.PixelToWorld(width, 0),
            transform.PixelToWorld(width, height),
            transform.PixelToWorld(0, height)
        };

        return new GeoBounds(points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X), points.Max(p => p.Y));
    }

    private static bool IsCogCandidate(Dataset dataset, IReadOnlyDictionary<string, string> metadata)
    {
        return dataset.GetRasterBand(1).GetOverviewCount() > 0
               && metadata.TryGetValue("IMAGE_STRUCTURE:LAYOUT", out var layout)
               && layout.Contains("COG", StringComparison.OrdinalIgnoreCase);
    }
}
