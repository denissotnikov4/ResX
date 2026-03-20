using MediatR;
using ResX.Common.Exceptions;
using ResX.Files.Application.Repositories;
using ResX.Files.Domain.AggregateRoots;
using ResX.Storage.S3.Abstractions;

namespace ResX.Files.Application.Queries.GetFileUrl;

public class GetFileUrlQueryHandler : IRequestHandler<GetFileUrlQuery, string>
{
    private readonly IFileRecordRepository _repository;
    private readonly IStorageService _storageService;

    public GetFileUrlQueryHandler(IFileRecordRepository repository, IStorageService storageService)
    {
        _repository = repository;
        _storageService = storageService;
    }

    public async Task<string> Handle(GetFileUrlQuery request, CancellationToken cancellationToken)
    {
        var fileRecord = await _repository.GetByIdAsync(request.FileId, cancellationToken)
                         ?? throw new NotFoundException(nameof(FileRecord), request.FileId);

        if (fileRecord.IsDeleted)
        {
            throw new NotFoundException(nameof(FileRecord), request.FileId);
        }

        var url = await _storageService.GetPresignedUrlAsync(
            fileKey: fileRecord.StorageKey,
            expiry: TimeSpan.FromHours(1),
            cancellationToken);

        return url;
    }
}