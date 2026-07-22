using Rasteria.Infrastructure.Coordinates;
using Rasteria.Raster;
using Microsoft.Extensions.DependencyInjection;

namespace Rasteria.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRasteriaInfrastructure(this IServiceCollection services)
    {
        services.AddRasterModule();
        services.AddSingleton<CoordinateService>();
        return services;
    }
}
