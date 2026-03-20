using ResX.Notifications.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Notifications.IntegrationTests.Collections;

[CollectionDefinition(Name)]
public sealed class NotificationsCollection : ICollectionFixture<NotificationsWebAppFactory>
{
    public const string Name = "Notifications";
}
