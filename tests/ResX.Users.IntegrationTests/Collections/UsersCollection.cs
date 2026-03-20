using ResX.Users.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Users.IntegrationTests.Collections;

[CollectionDefinition(Name)]
public sealed class UsersCollection : ICollectionFixture<UsersWebAppFactory>
{
    public const string Name = "Users";
}
