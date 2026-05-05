using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResX.EventBus.RabbitMQ;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Common.Persistence;
using ResX.Listings.API.Grpc;
using ResX.Messaging.Application.Repositories;
using ResX.Messaging.Application.Services;
using ResX.Messaging.Infrastructure.Grpc;
using ResX.Messaging.Infrastructure.Migrations;
using ResX.Messaging.Infrastructure.Persistence;
using ResX.Messaging.Infrastructure.Persistence.Repositories;
using ResX.Messaging.Infrastructure.Persistence.UnitOfWork;
using ResX.Users.API.Grpc;

namespace ResX.Messaging.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMessagingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<MessagingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("MessagingDb")));

        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(configuration.GetConnectionString("MessagingDb"))
                .ScanIn(typeof(M001_CreateMessagingTables).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        services.Configure<EventBusOptions>(configuration.GetSection(EventBusOptions.SectionName));
        services.AddSingleton<RabbitMQConnection>();
        services.AddSingleton<IEventBus, RabbitMQEventBus>();

        var usersGrpcUrl = configuration["Services:Users:GrpcUrl"]
            ?? throw new InvalidOperationException("Services:Users:GrpcUrl is not configured.");
        services.AddGrpcClient<UsersService.UsersServiceClient>(o =>
        {
            o.Address = new Uri(usersGrpcUrl);
        });
        services.AddScoped<IUsersClient, UsersGrpcClient>();

        var listingsGrpcUrl = configuration["Services:Listings:GrpcUrl"]
            ?? throw new InvalidOperationException("Services:Listings:GrpcUrl is not configured.");
        services.AddGrpcClient<ListingsService.ListingsServiceClient>(o =>
        {
            o.Address = new Uri(listingsGrpcUrl);
        });
        services.AddScoped<IListingsClient, ListingsGrpcClient>();

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
