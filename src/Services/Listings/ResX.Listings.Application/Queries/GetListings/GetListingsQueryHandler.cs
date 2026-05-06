using MediatR;
using ResX.Common.Caching;
using ResX.Common.Models;
using ResX.Listings.Application.DTOs;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Application.Services;
using ResX.Listings.Domain.Filters;

namespace ResX.Listings.Application.Queries.GetListings;

public class GetListingsQueryHandler : IRequestHandler<GetListingsQuery, PagedList<ListingPreviewDto>>
{
    private readonly IListingRepository _listingRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUsersClient _usersClient;
    private readonly ICacheService _cache;

    public GetListingsQueryHandler(
        IListingRepository listingRepository,
        ICategoryRepository categoryRepository,
        IUsersClient usersClient,
        ICacheService cache)
    {
        _listingRepository = listingRepository;
        _categoryRepository = categoryRepository;
        _usersClient = usersClient;
        _cache = cache;
    }

    public async Task<PagedList<ListingPreviewDto>> Handle(GetListingsQuery request, CancellationToken cancellationToken)
    {
        var version = await _cache.GetAsync<int>("listings:version", cancellationToken);
        var cacheKey = $"listings:v{version}:p{request.PageNumber}:s{request.PageSize}:cat{request.CategoryId}:cond{request.Condition}:tr{request.TransferType}:city{request.City}:q{request.SearchQuery}";

        var cachedPage = await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var filter = new ListingFilter(
                request.CategoryId,
                request.Condition,
                request.TransferType,
                request.City,
                DonorId: null,
                request.SearchQuery);

            var pagedListings = await _listingRepository.GetPagedAsync(
                filter,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var previews = pagedListings.Items.Select(listing => new CachedListingPreview(
                listing.Id,
                listing.Title,
                listing.CategoryId,
                listing.Condition.ToString(),
                listing.TransferType.ToString(),
                listing.Status.ToString(),
                listing.Location.City,
                listing.Photos.OrderBy(p => p.DisplayOrder).FirstOrDefault()?.Url,
                listing.DonorId,
                listing.ViewCount,
                listing.WeightGrams,
                listing.Co2SavedG,
                listing.WasteSavedG,
                listing.CreatedAt)).ToList();

            return new PagedList<CachedListingPreview>(
                previews.AsReadOnly(),
                pagedListings.TotalCount,
                request.PageNumber,
                request.PageSize);
        }, expiry: TimeSpan.FromMinutes(5), cancellationToken);

        var donorIds = cachedPage.Items.Select(i => i.DonorId).Distinct().ToList();
        var categoryIds = cachedPage.Items.Select(i => i.CategoryId).Distinct().ToList();

        var donorsTask = _usersClient.GetDonorsAsync(donorIds, cancellationToken);
        var categoriesTask = _categoryRepository.GetByIdsAsync(categoryIds, cancellationToken);
        await Task.WhenAll(donorsTask, categoriesTask);

        var donors = donorsTask.Result;
        var categories = categoriesTask.Result.ToDictionary(c => c.Id);

        var listingPreviewDtos = cachedPage.Items
            .Select(i =>
            {
                var category = categories.TryGetValue(i.CategoryId, out var c)
                    ? new CategoryDto(c.Id, c.Name, c.ParentCategoryId)
                    : new CategoryDto(i.CategoryId, "(deleted)", null);

                return new ListingPreviewDto(
                    i.Id,
                    i.Title,
                    category,
                    i.Condition,
                    i.TransferType,
                    i.Status,
                    i.City,
                    i.ThumbnailUrl,
                    donors.TryGetValue(i.DonorId, out var donor) ? donor : null,
                    i.ViewCount,
                    i.WeightGrams,
                    i.Co2SavedG,
                    i.WasteSavedG,
                    i.CreatedAt);
            })
            .ToList()
            .AsReadOnly();

        return new PagedList<ListingPreviewDto>(
            listingPreviewDtos,
            cachedPage.TotalCount,
            request.PageNumber,
            request.PageSize);
    }

    private record CachedListingPreview(
        Guid Id,
        string Title,
        Guid CategoryId,
        string Condition,
        string TransferType,
        string Status,
        string City,
        string? ThumbnailUrl,
        Guid DonorId,
        int ViewCount,
        int WeightGrams,
        int Co2SavedG,
        int WasteSavedG,
        DateTime CreatedAt);
}
