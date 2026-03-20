using ResX.Disputes.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Disputes.IntegrationTests.Collections;

[CollectionDefinition(Name)]
public sealed class DisputesCollection : ICollectionFixture<DisputesWebAppFactory>
{
    public const string Name = "Disputes";
}
