using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Files.Application.Commands.DeleteFile;
using ResX.Files.Application.Repositories;
using ResX.Files.Domain.AggregateRoots;
using ResX.Storage.S3.Abstractions;
using Xunit;

namespace ResX.Files.UnitTests.Commands;

public class DeleteFileCommandHandlerTests
{
    private readonly IFileRecordRepository _repository = Substitute.For<IFileRecordRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly ILogger<DeleteFileCommandHandler> _logger = Substitute.For<ILogger<DeleteFileCommandHandler>>();

    private DeleteFileCommandHandler CreateSut() => new(_repository, _uow, _storage, _logger);

    [Fact]
    public async Task Handle_FileNotFound_ThrowsNotFoundException()
    {
        var cmd = new DeleteFileCommand(Guid.NewGuid(), Guid.NewGuid());
        _repository.GetByIdAsync(cmd.FileId, Arg.Any<CancellationToken>()).Returns((FileRecord?)null);

        var act = async () => await CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DifferentOwner_ThrowsForbiddenException()
    {
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var record = FileRecord.Create("a.txt", "k", "u", "text/plain", 10, owner);
        _repository.GetByIdAsync(record.Id, Arg.Any<CancellationToken>()).Returns(record);
        var cmd = new DeleteFileCommand(record.Id, other);

        var act = async () => await CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OwnerDeletes_DeletesFromStorageAndMarks()
    {
        var owner = Guid.NewGuid();
        var record = FileRecord.Create("a.txt", "k", "u", "text/plain", 10, owner);
        _repository.GetByIdAsync(record.Id, Arg.Any<CancellationToken>()).Returns(record);
        var cmd = new DeleteFileCommand(record.Id, owner);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        record.IsDeleted.Should().BeTrue();
        await _storage.Received(1).DeleteAsync("k", Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
