using MaxRev.Gdal.Core;
using OSGeo.GDAL;

namespace Rasteria.Raster.Gdal;

internal static class GdalRuntime
{
    private static readonly Lazy<bool> Configured = new(() =>
    {
        GdalBase.ConfigureAll();
        OSGeo.GDAL.Gdal.AllRegister();
        return true;
    });

    public static void EnsureConfigured()
    {
        _ = Configured.Value;
    }
}
