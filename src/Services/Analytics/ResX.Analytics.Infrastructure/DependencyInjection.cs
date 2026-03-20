using Microsoft.Extensions.DependencyInjection;
using ResX.Analytics.Application.Repositories;
using ResX.Analytics.Infrastructure.Persistence;

namespace ResX.Analytics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
        return services;
    }
}
