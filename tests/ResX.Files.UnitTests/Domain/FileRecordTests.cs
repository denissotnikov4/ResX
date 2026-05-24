using FluentAssertions;
using ResX.Files.Domain.AggregateRoots;
using Xunit;

namespace ResX.Files.UnitTests.Domain;

public class FileRecordTests
{
    [Fact]
    public void Create_ShouldInitializeAllProperties()
    {
        var uploadedBy = Guid.NewGuid();
        var beforeCreate = DateTime.UtcNow.AddSeconds(-1);

        var record = FileRecord.Create("file.txt", "key123", "https://url", "text/plain", 100, uploadedBy);

        record.Id.Should().NotBe(Guid.Empty);
        record.OriginalName.Should().Be("file.txt");
        record.StorageKey.Should().Be("key123");
        record.Url.Should().Be("https://url");
        record.ContentType.Should().Be("text/plain");
        record.SizeBytes.Should().Be(100);
        record.UploadedBy.Should().Be(uploadedBy);
        record.IsDeleted.Should().BeFalse();
        record.CreatedAt.Should().BeAfter(beforeCreate);
    }

    [Fact]
    public void MarkDeleted_ShouldSetIsDeletedTrue()
    {
        var record = FileRecord.Create("a.png", "k", "u", "image/png", 10, Guid.NewGuid());

        record.MarkDeleted();

        record.IsDeleted.Should().BeTrue();
    }
}
