using ResX.Charity.Domain.AggregateRoots;

namespace ResX.Charity.Application.Repositories;

public interface ICharityRequestRepository
{
    Task<CharityRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<CharityRequest>> GetActiveAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetActiveTotalCountAsync(CancellationToken cancellationToken = default);

    Task AddAsync(CharityRequest request, CancellationToken cancellationToken = default);
}