using Microsoft.EntityFrameworkCore;
using ResX.Charity.Application.Repositories;
using ResX.Charity.Domain.AggregateRoots;

namespace ResX.Charity.Infrastructure.Persistence.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly CharityDbContext _context;
    public OrganizationRepository(CharityDbContext context) => _context = context;

    public async Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Organizations.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<Organization?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.Organizations.FirstOrDefaultAsync(o => o.UserId == userId, cancellationToken);

    public Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        _context.Organizations.Add(organization);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(organization).State == EntityState.Detached)
            _context.Organizations.Update(organization);
        return Task.CompletedTask;
    }
}