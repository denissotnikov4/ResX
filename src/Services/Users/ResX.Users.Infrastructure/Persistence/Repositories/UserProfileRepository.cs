using Microsoft.EntityFrameworkCore;
using ResX.Common.Models;
using ResX.Users.Application.Repositories;
using ResX.Users.Domain.Aggregates;
using ResX.Users.Domain.Entities;

namespace ResX.Users.Infrastructure.Persistence.Repositories;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly UsersDbContext _context;

    public UserProfileRepository(UsersDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public Task AddAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
    {
        _context.UserProfiles.Add(userProfile);
        return Task.CompletedTask;
    }

    public async Task<PagedList<Review>> GetReviewsAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Reviews
            .Where(r => r.UserProfileId == userId)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<Review>(items.AsReadOnly(), totalCount, pageNumber, pageSize);
    }

    public async Task<PagedList<UserProfile>> GetLeaderboardAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UserProfiles
            .OrderByDescending(p => p.EcoStats.Co2SavedKg + p.EcoStats.WasteSavedKg);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<UserProfile>(items.AsReadOnly(), totalCount, pageNumber, pageSize);
    }
}