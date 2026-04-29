using ResX.Listings.Domain.AggregateRoots;
using ResX.Listings.Domain.Entities;

namespace ResX.Listings.Application.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    Task<bool> HasListingsAsync(Guid categoryId, CancellationToken cancellationToken = default);

    Task AddHistoryAsync(CategoryHistory entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryHistory>> GetHistoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);
}
