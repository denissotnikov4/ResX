using ResX.Common.Models;
using ResX.Listings.Domain.AggregateRoots;
using ResX.Listings.Domain.Filters;

namespace ResX.Listings.Application.Repositories;

public interface IListingRepository
{
    Task<Listing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Listing>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task<PagedList<Listing>> GetPagedAsync(ListingFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    
    Task AddAsync(Listing listing, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task IncrementViewCountAsync(Guid id, CancellationToken cancellationToken = default);
}
