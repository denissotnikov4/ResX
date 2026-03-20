using ResX.Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Messaging.IntegrationTests.Collections;

[CollectionDefinition(Name)]
public sealed class MessagingCollection : ICollectionFixture<MessagingWebAppFactory>
{
    public const string Name = "Messaging";
}
