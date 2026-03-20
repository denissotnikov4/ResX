using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResX.Common.Persistence;
using ResX.Disputes.Application.Repositories;
using ResX.Disputes.Infrastructure.Migrations;
using ResX.Disputes.Infrastructure.Persistence;
using ResX.Disputes.Infrastructure.Persistence.Repositories;
using ResX.Disputes.Infrastructure.Persistence.UnitOfWork;

namespace ResX.Disputes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDisputesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DisputesDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DisputesDb")));

        services.AddScoped<IDisputeRepository, DisputeRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddFluentMigratorCore()
            .ConfigureRunner(r => 
                r.AddPostgres().WithGlobalConnectionString(configuration.GetConnectionString("DisputesDb"))
                    .ScanIn(typeof(M001_CreateDisputesTables).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        return services;
    }

    public static IServiceProvider RunMigrations(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();
        return serviceProvider;
    }
}
