using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Files.Application.Repositories;
using ResX.Files.Domain.AggregateRoots;
using ResX.Storage.S3.Abstractions;

namespace ResX.Files.Application.Commands.DeleteFile;

public class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand, Unit>
{
    private readonly IFileRecordRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly ILogger<DeleteFileCommandHandler> _logger;

    public DeleteFileCommandHandler(
        IFileRecordRepository repository,
        IUnitOfWork unitOfWork,
        IStorageService storageService,
        ILogger<DeleteFileCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        var fileRecord = await _repository.GetByIdAsync(request.FileId, cancellationToken)
                         ?? throw new NotFoundException(nameof(FileRecord), request.FileId);

        if (fileRecord.UploadedBy != request.RequestingUserId)
        {
            throw new ForbiddenException("You can only delete your own files.");
        }

        await _storageService.DeleteAsync(fileRecord.StorageKey, cancellationToken);
        fileRecord.MarkDeleted();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("File {FileId} deleted.", request.FileId);

        return Unit.Value;
    }
}