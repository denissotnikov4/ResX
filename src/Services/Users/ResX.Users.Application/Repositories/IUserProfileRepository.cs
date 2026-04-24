using ResX.Common.Models;
using ResX.Users.Domain.Aggregates;
using ResX.Users.Domain.Entities;

namespace ResX.Users.Application.Repositories;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserProfile>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task AddAsync(UserProfile userProfile, CancellationToken cancellationToken = default);

    Task<PagedList<Review>> GetReviewsAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedList<UserProfile>> GetLeaderboardAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}