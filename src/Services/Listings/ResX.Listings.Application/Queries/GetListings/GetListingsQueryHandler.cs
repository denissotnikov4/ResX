using MediatR;
using ResX.Common.Caching;
using ResX.Common.Models;
using ResX.Listings.Application.DTOs;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Domain.Filters;

namespace ResX.Listings.Application.Queries.GetListings;

public class GetListingsQueryHandler : IRequestHandler<GetListingsQuery, PagedList<ListingPreviewDto>>
{
    private readonly IListingRepository _listingRepository;
    private readonly ICacheService _cache;

    public GetListingsQueryHandler(IListingRepository listingRepository, ICacheService cache)
    {
        _listingRepository = listingRepository;
        _cache = cache;
    }

    public async Task<PagedList<ListingPreviewDto>> Handle(GetListingsQuery request, CancellationToken cancellationToken)
    {
        var version = await _cache.GetAsync<int>("listings:version", cancellationToken);
        var cacheKey = $"listings:v{version}:p{request.PageNumber}:s{request.PageSize}:cat{request.CategoryId}:cond{request.Condition}:tr{request.TransferType}:city{request.City}:q{request.SearchQuery}";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
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

            var listingPreviewDtos = pagedListings.Items.Select(listing => new ListingPreviewDto(
                listing.Id,
                listing.Title,
                new CategoryDto(listing.Category.Id, listing.Category.Name, listing.Category.ParentCategoryId),
                listing.Condition.ToString(),
                listing.TransferType.ToString(),
                listing.Status.ToString(),
                listing.Location.City,
                listing.Photos.OrderBy(p => p.DisplayOrder).FirstOrDefault()?.Url,
                listing.ViewCount,
                listing.CreatedAt)).ToList().AsReadOnly();

            return new PagedList<ListingPreviewDto>(
                listingPreviewDtos,
                pagedListings.TotalCount,
                request.PageNumber,
                request.PageSize);
        }, expiry: TimeSpan.FromMinutes(5), cancellationToken);
    }
}
