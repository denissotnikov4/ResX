using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResX.Caching.Redis;
using ResX.Common.Caching;
using ResX.EventBus.RabbitMQ;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Common.Persistence;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Infrastructure.Migrations;
using ResX.Listings.Infrastructure.Persistence;
using ResX.Listings.Infrastructure.Persistence.Repositories;
using ResX.Listings.Infrastructure.Persistence.UnitOfWork;
using StackExchange.Redis;

namespace ResX.Listings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddListingsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ListingsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ListingsDb")));

        services.AddScoped<IListingRepository, ListingRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(configuration.GetConnectionString("ListingsDb"))
                .ScanIn(typeof(M001_CreateListingsTable).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        // Lazy factory: connection is established on first resolution, not at registration time.
        // This allows ConfigureTestServices in tests to replace IConnectionMultiplexer before it is used.
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? "localhost:6379"));
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.AddSingleton<ICacheService, RedisCacheService>();

        services.Configure<EventBusOptions>(configuration.GetSection(EventBusOptions.SectionName));
        services.AddSingleton<RabbitMQConnection>();
        services.AddSingleton<IEventBus, RabbitMQEventBus>();

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
