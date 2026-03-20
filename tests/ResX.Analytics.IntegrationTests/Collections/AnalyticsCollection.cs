using ResX.Analytics.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Analytics.IntegrationTests.Collections;

[CollectionDefinition(Name)]
public sealed class AnalyticsCollection : ICollectionFixture<AnalyticsWebAppFactory>
{
    public const string Name = "Analytics";
}
