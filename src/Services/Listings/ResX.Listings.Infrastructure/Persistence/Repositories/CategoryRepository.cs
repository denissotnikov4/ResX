using Microsoft.EntityFrameworkCore;
using ResX.Listings.Application.DTOs;
using ResX.Listings.Application.Repositories;

namespace ResX.Listings.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ListingsDbContext _context;

    public CategoryRepository(ListingsDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CategoryResultDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var results = await _context.Database
            .SqlQuery<CategoryResultDto>(
                $"SELECT id, name, description, parent_category_id, display_order FROM listings.categories WHERE is_active = true ORDER BY display_order")
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }
}
