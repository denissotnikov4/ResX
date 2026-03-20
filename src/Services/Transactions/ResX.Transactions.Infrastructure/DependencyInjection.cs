using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResX.EventBus.RabbitMQ;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Common.Persistence;
using ResX.Transactions.Application.Repositories;
using ResX.Transactions.Infrastructure.Migrations;
using ResX.Transactions.Infrastructure.Persistence;
using ResX.Transactions.Infrastructure.Persistence.Repositories;
using ResX.Transactions.Infrastructure.Persistence.UnitOfWork;

namespace ResX.Transactions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<TransactionsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("TransactionsDb")));

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(configuration.GetConnectionString("TransactionsDb"))
                .ScanIn(typeof(M001_CreateTransactionsTable).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

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
