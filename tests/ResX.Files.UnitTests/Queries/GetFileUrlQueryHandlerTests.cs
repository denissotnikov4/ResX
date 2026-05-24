using FluentAssertions;
using NSubstitute;
using ResX.Common.Exceptions;
using ResX.Files.Application.Queries.GetFileUrl;
using ResX.Files.Application.Repositories;
using ResX.Files.Domain.AggregateRoots;
using ResX.Storage.S3.Abstractions;
using Xunit;

namespace ResX.Files.UnitTests.Queries;

public class GetFileUrlQueryHandlerTests
{
    private readonly IFileRecordRepository _repository = Substitute.For<IFileRecordRepository>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();

    private GetFileUrlQueryHandler CreateSut() => new(_repository, _storage);

    [Fact]
    public async Task Handle_NotFound_ThrowsNotFoundException()
    {
        var q = new GetFileUrlQuery(Guid.NewGuid());
        _repository.GetByIdAsync(q.FileId, Arg.Any<CancellationToken>()).Returns((FileRecord?)null);

        var act = async () => await CreateSut().Handle(q, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_FileDeleted_ThrowsNotFoundException()
    {
        var record = FileRecord.Create("f.txt", "k", "u", "text/plain", 10, Guid.NewGuid());
        record.MarkDeleted();
        _repository.GetByIdAsync(record.Id, Arg.Any<CancellationToken>()).Returns(record);

        var act = async () => await CreateSut().Handle(new GetFileUrlQuery(record.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidFile_ReturnsPresignedUrl()
    {
        var record = FileRecord.Create("f.txt", "key1", "old-url", "text/plain", 10, Guid.NewGuid());
        _repository.GetByIdAsync(record.Id, Arg.Any<CancellationToken>()).Returns(record);
        _storage.GetPresignedUrlAsync("key1", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns("https://signed");

        var url = await CreateSut().Handle(new GetFileUrlQuery(record.Id), CancellationToken.None);

        url.Should().Be("https://signed");
    }
}
