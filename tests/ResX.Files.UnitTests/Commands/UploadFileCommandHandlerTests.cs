using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Files.Application.Commands.UploadFile;
using ResX.Files.Application.Repositories;
using ResX.Files.Domain.AggregateRoots;
using ResX.Storage.S3.Abstractions;
using Xunit;

namespace ResX.Files.UnitTests.Commands;

public class UploadFileCommandHandlerTests
{
    private readonly IFileRecordRepository _repository = Substitute.For<IFileRecordRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly ILogger<UploadFileCommandHandler> _logger = Substitute.For<ILogger<UploadFileCommandHandler>>();

    private UploadFileCommandHandler CreateSut() => new(_repository, _uow, _storage, _logger);

    [Fact]
    public async Task Handle_ValidFile_UploadsAndReturnsDto()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var userId = Guid.NewGuid();
        var cmd = new UploadFileCommand(stream, "f.txt", "text/plain", 3, userId);
        _storage.UploadAsync(stream, "f.txt", "text/plain", Arg.Any<CancellationToken>()).Returns("key-1");
        _storage.GetPresignedUrlAsync("key-1", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns("https://url");

        var dto = await CreateSut().Handle(cmd, CancellationToken.None);

        dto.Should().NotBeNull();
        dto.OriginalName.Should().Be("f.txt");
        dto.StorageKey.Should().Be("key-1");
        dto.Url.Should().Be("https://url");
        dto.UploadedBy.Should().Be(userId);
        await _repository.Received(1).AddAsync(Arg.Any<FileRecord>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FileTooLarge_ThrowsDomainException()
    {
        using var stream = new MemoryStream();
        var cmd = new UploadFileCommand(stream, "big.bin", "app/octet", 101L * 1024 * 1024, Guid.NewGuid());

        var act = async () => await CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*100 MB*");
        await _storage.DidNotReceive().UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
