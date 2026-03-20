using ResX.Files.Domain.AggregateRoots;

namespace ResX.Files.Application.Repositories;

public interface IFileRecordRepository
{
    Task<FileRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(FileRecord fileRecord, CancellationToken cancellationToken = default);
}