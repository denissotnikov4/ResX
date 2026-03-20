using ResX.Identity.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Identity.IntegrationTests.Collections;

/// <summary>
/// xUnit collection that shares a single IdentityWebAppFactory (and its PostgreSQL container)
/// across all Identity test classes. This means the container starts once for the whole assembly.
/// Each test class resets the database via Respawn in its IAsyncLifetime.InitializeAsync.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IdentityCollection : ICollectionFixture<IdentityWebAppFactory>
{
    public const string Name = "Identity Integration Tests";
}
