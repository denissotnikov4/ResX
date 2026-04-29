using Microsoft.EntityFrameworkCore;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Domain.AggregateRoots;
using ResX.Listings.Domain.Entities;

namespace ResX.Listings.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ListingsDbContext _context;

    public CategoryRepository(ListingsDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return Array.Empty<Category>();

        return await _context.Categories
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        _context.Categories.Add(category);
        return Task.CompletedTask;
    }

    public Task<bool> HasListingsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return _context.Listings.AnyAsync(l => l.CategoryId == categoryId, cancellationToken);
    }

    public Task AddHistoryAsync(CategoryHistory entry, CancellationToken cancellationToken = default)
    {
        _context.CategoryHistories.Add(entry);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<CategoryHistory>> GetHistoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CategoryHistories
            .Where(h => h.CategoryId == categoryId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
    }
}
