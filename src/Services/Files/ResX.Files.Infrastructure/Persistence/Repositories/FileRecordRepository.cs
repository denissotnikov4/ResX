using Microsoft.EntityFrameworkCore;
using ResX.Files.Application.Repositories;
using ResX.Files.Domain.AggregateRoots;

namespace ResX.Files.Infrastructure.Persistence.Repositories;

public class FileRecordRepository : IFileRecordRepository
{
    private readonly FilesDbContext _context;

    public FileRecordRepository(FilesDbContext context)
    {
        _context = context;
    }

    public async Task<FileRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FileRecords.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public Task AddAsync(FileRecord fileRecord, CancellationToken cancellationToken = default)
    {
        _context.FileRecords.Add(fileRecord);
        return Task.CompletedTask;
    }
}