using Microsoft.EntityFrameworkCore;
using ResX.Disputes.Application.Repositories;
using ResX.Disputes.Domain.AggregateRoots;
using ResX.Disputes.Domain.Entities;
using ResX.Disputes.Domain.Enums;

namespace ResX.Disputes.Infrastructure.Persistence.Repositories;

public class DisputeRepository : IDisputeRepository
{
    private readonly DisputesDbContext _context;

    public DisputeRepository(DisputesDbContext context)
    {
        _context = context;
    }

    public async Task<Dispute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Disputes
            .Include(d => d.Evidences)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<List<Dispute>> GetByUserIdAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _context.Disputes
            .Include(d => d.Evidences)
            .Where(d => d.InitiatorId == userId || d.RespondentId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Dispute>> GetAllAsync(int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _context.Disputes
            .Include(d => d.Evidences)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Dispute>> GetOpenDisputesAsync(int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _context.Disputes
            .Include(d => d.Evidences)
            .Where(d => d.Status == DisputeStatus.Open || d.Status == DisputeStatus.UnderReview)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Dispute dispute, CancellationToken cancellationToken = default)
    {
        _context.Disputes.Add(dispute);
        return Task.CompletedTask;
    }

    public Task AddEvidenceAsync(Evidence evidence, CancellationToken cancellationToken = default)
    {
        _context.Evidence.Add(evidence);
        return Task.CompletedTask;
    }
}