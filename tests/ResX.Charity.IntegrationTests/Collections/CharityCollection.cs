using ResX.Charity.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Charity.IntegrationTests.Collections;

[CollectionDefinition(Name)]
public sealed class CharityCollection : ICollectionFixture<CharityWebAppFactory>
{
    public const string Name = "Charity";
}
