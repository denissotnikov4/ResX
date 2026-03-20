using ResX.Charity.Domain.AggregateRoots;

namespace ResX.Charity.Application.Repositories;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Organization?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(Organization organization, CancellationToken cancellationToken = default);

    Task UpdateAsync(Organization organization, CancellationToken cancellationToken = default);
}