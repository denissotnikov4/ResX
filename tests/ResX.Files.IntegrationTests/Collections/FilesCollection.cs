using ResX.Files.IntegrationTests.Fixtures;
using Xunit;

namespace ResX.Files.IntegrationTests.Collections;

[CollectionDefinition(Name)]
public sealed class FilesCollection : ICollectionFixture<FilesWebAppFactory>
{
    public const string Name = "Files";
}
