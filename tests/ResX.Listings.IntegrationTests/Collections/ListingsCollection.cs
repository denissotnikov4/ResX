using ResX.Listings.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Listings.IntegrationTests.Collections;

[CollectionDefinition(Name)]
public sealed class ListingsCollection : ICollectionFixture<ListingsWebAppFactory>
{
    public const string Name = "Listings Integration Tests";
}
