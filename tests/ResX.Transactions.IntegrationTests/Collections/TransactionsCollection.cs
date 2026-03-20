using ResX.Transactions.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Transactions.IntegrationTests.Collections;

[CollectionDefinition(Name)]
public sealed class TransactionsCollection : ICollectionFixture<TransactionsWebAppFactory>
{
    public const string Name = "Transactions Integration Tests";
}
