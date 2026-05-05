using Microsoft.EntityFrameworkCore;
using ResX.Common.Models;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Domain.AggregateRoots;
using ResX.Listings.Domain.Entities;
using ResX.Listings.Domain.Enums;
using ResX.Listings.Domain.Filters;

namespace ResX.Listings.Infrastructure.Persistence.Repositories;

public class ListingRepository : IListingRepository
{
    private readonly ListingsDbContext _context;

    public ListingRepository(ListingsDbContext context)
    {
        _context = context;
    }

    public async Task<Listing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Listings
            .Include(l => l.Photos)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Listing>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return Array.Empty<Listing>();

        return await _context.Listings
            .Where(l => ids.Contains(l.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedList<Listing>> GetPagedAsync(
        ListingFilter filter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Listings
            .Include(l => l.Photos)
            .AsQueryable();

        if (!filter.IncludeAllStatuses)
        {
            query = query.Where(l => l.Status == ListingStatus.Active);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(l => l.CategoryId == filter.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Condition) &&
            Enum.TryParse<ItemCondition>(filter.Condition, out var condition))
        {
            query = query.Where(l => l.Condition == condition);
        }

        if (!string.IsNullOrWhiteSpace(filter.TransferType) &&
            Enum.TryParse<TransferType>(filter.TransferType, out var transferType))
        {
            query = query.Where(l => l.TransferType == transferType);
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            query = query.Where(l => l.Location.City.ToLower() == filter.City.ToLower());
        }

        if (filter.DonorId.HasValue)
        {
            query = query.Where(l => l.DonorId == filter.DonorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
        {
            var searchLower = filter.SearchQuery.ToLower();
            query = query.Where(l =>
                l.Title.ToLower().Contains(searchLower) ||
                l.Description.ToLower().Contains(searchLower));
        }

        query = query.OrderByDescending(l => l.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<Listing>(items.AsReadOnly(), totalCount, pageNumber, pageSize);
    }

    public Task AddAsync(Listing listing, CancellationToken cancellationToken = default)
    {
        _context.Listings.Add(listing);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var listing = _context.Listings.Find(id);
        if (listing != null)
            _context.Listings.Remove(listing);

        return Task.CompletedTask;
    }

    public async Task IncrementViewCountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _context.Listings
            .Where(l => l.Id == id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(l => l.ViewCount, l => l.ViewCount + 1),
                cancellationToken);
    }
}
