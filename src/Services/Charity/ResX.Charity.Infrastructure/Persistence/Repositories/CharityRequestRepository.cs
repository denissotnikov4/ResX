using Microsoft.EntityFrameworkCore;
using ResX.Charity.Application.Repositories;
using ResX.Charity.Domain.AggregateRoots;

namespace ResX.Charity.Infrastructure.Persistence.Repositories;

public class CharityRequestRepository : ICharityRequestRepository
{
    private readonly CharityDbContext _context;

    public CharityRequestRepository(CharityDbContext context)
    {
        _context = context;
    }

    public async Task<CharityRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CharityRequests
            .Include(r => r.RequestedItems)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<List<CharityRequest>> GetActiveAsync(
        int pageNumber, 
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _context.CharityRequests
            .Include(r => r.RequestedItems)
            .Where(r => r.Status == Domain.Enums.CharityRequestStatus.Active)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetActiveTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CharityRequests
            .CountAsync(r => r.Status == Domain.Enums.CharityRequestStatus.Active, cancellationToken);
    }

    public Task AddAsync(CharityRequest request, CancellationToken cancellationToken = default)
    {
        _context.CharityRequests.Add(request);
        return Task.CompletedTask;
    }
}