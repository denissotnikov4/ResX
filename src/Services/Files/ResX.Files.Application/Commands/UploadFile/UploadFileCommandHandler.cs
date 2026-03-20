using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Files.Application.DTOs;
using ResX.Files.Application.Repositories;
using ResX.Files.Domain.AggregateRoots;
using ResX.Storage.S3.Abstractions;

namespace ResX.Files.Application.Commands.UploadFile;

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, FileRecordDto>
{
    private readonly IFileRecordRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly ILogger<UploadFileCommandHandler> _logger;

    public UploadFileCommandHandler(
        IFileRecordRepository repository,
        IUnitOfWork unitOfWork,
        IStorageService storageService,
        ILogger<UploadFileCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<FileRecordDto> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        const long maxSizeBytes = 100 * 1024 * 1024; // 100 MB
        if (request.SizeBytes > maxSizeBytes)
        {
            throw new DomainException("File size exceeds the maximum allowed limit of 100 MB.");
        }

        var storageKey = await _storageService.UploadAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            cancellationToken);

        var url = await _storageService.GetPresignedUrlAsync(
            fileKey: storageKey,
            expiry: TimeSpan.FromDays(365),
            cancellationToken);

        var fileRecord = FileRecord.Create(
            request.FileName,
            storageKey,
            url,
            request.ContentType,
            request.SizeBytes,
            request.UploadedBy);

        await _repository.AddAsync(fileRecord, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("File {FileId} uploaded by user {UserId}.", fileRecord.Id, request.UploadedBy);

        return new FileRecordDto(
            fileRecord.Id,
            fileRecord.OriginalName,
            fileRecord.StorageKey,
            fileRecord.Url,
            fileRecord.ContentType,
            fileRecord.SizeBytes,
            fileRecord.UploadedBy,
            fileRecord.CreatedAt);
    }
}