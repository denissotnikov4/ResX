using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResX.Charity.Application.Commands;
using ResX.Charity.Application.Repositories;
using ResX.Charity.Infrastructure.Migrations;
using ResX.Charity.Infrastructure.Persistence;
using ResX.Charity.Infrastructure.Persistence.Repositories;
using ResX.Charity.Infrastructure.Persistence.UnitOfWork;
using ResX.Common.Persistence;

namespace ResX.Charity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCharityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<CharityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("CharityDb")));

        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<ICharityRequestRepository, CharityRequestRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(configuration.GetConnectionString("CharityDb"))
                .ScanIn(typeof(M001_CreateCharityTables).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        return services;
    }

    public static IServiceProvider RunMigrations(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
        return serviceProvider;
    }
}
