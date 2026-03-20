using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ResX.EventBus.RabbitMQ;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.IntegrationTests.Common.Fixtures;
using ResX.IntegrationTests.Common.Helpers;
using ResX.Notifications.Application.Commands;
using ResX.Notifications.Application.Commands.CreateNotification;
using ResX.Notifications.Domain.Enums;
using Xunit;

namespace ResX.Notifications.IntegrationTests.Fixtures;

public sealed class NotificationsWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres = new();
    private bool _respawnerReady;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotificationsDb"] = _postgres.ConnectionString,
                ["Jwt:SecretKey"] = JwtTokenHelper.TestSecretKey,
                ["Jwt:Issuer"] = JwtTokenHelper.TestIssuer,
                ["Jwt:Audience"] = JwtTokenHelper.TestAudience,
                ["Jwt:ExpiryMinutes"] = "60",
                ["RabbitMQ:HostName"] = "localhost",
                ["RabbitMQ:UserName"] = "guest",
                ["RabbitMQ:Password"] = "guest",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<RabbitMQConnection>();
            services.RemoveAll<IEventBus>();
            services.AddSingleton(Substitute.For<IEventBus>());
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.InitializeAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__NotificationsDb", _postgres.ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__SecretKey", JwtTokenHelper.TestSecretKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtTokenHelper.TestIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", JwtTokenHelper.TestAudience);
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", "60");

        _ = CreateClient();
        await _postgres.InitializeRespawnerAsync(["notifications"]);
        _respawnerReady = true;
    }

    /// <summary>Seeds a notification for a user via CreateNotificationCommand.</summary>
    public async Task<Guid> SeedNotificationAsync(Guid userId, string title = "Тест", string body = "Тестовое уведомление")
    {
        using var scope = Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new CreateNotificationCommand(userId, NotificationType.SystemNotification, title, body));
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawnerReady)
        {
            await _postgres.ResetAsync();
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__NotificationsDb", null);
        Environment.SetEnvironmentVariable("Jwt__SecretKey", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", null);
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
