using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResX.Common.Persistence;
using ResX.Files.Application.Repositories;
using ResX.Files.Infrastructure.Migrations;
using ResX.Files.Infrastructure.Persistence;
using ResX.Files.Infrastructure.Persistence.Repositories;
using ResX.Files.Infrastructure.Persistence.UnitOfWork;
using ResX.Storage.S3;
using ResX.Storage.S3.Abstractions;

namespace ResX.Files.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFilesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<FilesDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("FilesDb")));

        services.AddScoped<IFileRecordRepository, FileRecordRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));
        services.AddSingleton<IStorageService, S3StorageService>();

        services.AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(configuration.GetConnectionString("FilesDb"))
                .ScanIn(typeof(M001_CreateFileRecordsTable).Assembly).For.Migrations())
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
