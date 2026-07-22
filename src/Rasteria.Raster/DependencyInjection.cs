using Rasteria.Core.Rasters;
using Rasteria.Raster.Gdal;
using Microsoft.Extensions.DependencyInjection;

namespace Rasteria.Raster;

public static class DependencyInjection
{
    public static IServiceCollection AddRasterModule(this IServiceCollection services)
    {
        services.AddSingleton<GdalRasterSourceFactory>();
        services.AddSingleton<IRasterSourceFactory>(sp => sp.GetRequiredService<GdalRasterSourceFactory>());
        services.AddSingleton<ITileSourceFactory>(sp => sp.GetRequiredService<GdalRasterSourceFactory>());
        return services;
    }
}
