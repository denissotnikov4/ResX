using MediatR;
using ResX.Common.Models;
using ResX.Listings.Application.DTOs;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Domain.Filters;

namespace ResX.Listings.Application.Queries.GetMyListings;

public class GetMyListingsQueryHandler : IRequestHandler<GetMyListingsQuery, PagedList<ListingPreviewDto>>
{
    private readonly IListingRepository _listingRepository;
    private readonly ICategoryRepository _categoryRepository;

    public GetMyListingsQueryHandler(
        IListingRepository listingRepository,
        ICategoryRepository categoryRepository)
    {
        _listingRepository = listingRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<PagedList<ListingPreviewDto>> Handle(GetMyListingsQuery request, CancellationToken cancellationToken)
    {
        var filter = new ListingFilter(DonorId: request.DonorId, IncludeAllStatuses: true);

        var pagedListings = await _listingRepository.GetPagedAsync(
            filter,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var categoryIds = pagedListings.Items.Select(l => l.CategoryId).Distinct().ToList();
        var categories = (await _categoryRepository.GetByIdsAsync(categoryIds, cancellationToken))
            .ToDictionary(c => c.Id);

        var listingPreviewDtos = pagedListings.Items.Select(listing =>
        {
            var category = categories.TryGetValue(listing.CategoryId, out var c)
                ? new CategoryDto(c.Id, c.Name, c.ParentCategoryId)
                : new CategoryDto(listing.CategoryId, "(deleted)", null);

            return new ListingPreviewDto(
                listing.Id,
                listing.Title,
                category,
                listing.Condition.ToString(),
                listing.TransferType.ToString(),
                listing.Status.ToString(),
                listing.Location.City,
                listing.Photos.OrderBy(p => p.DisplayOrder).FirstOrDefault()?.Url,
                Donor: null,
                listing.ViewCount,
                listing.CreatedAt);
        }).ToList().AsReadOnly();

        return new PagedList<ListingPreviewDto>(listingPreviewDtos, pagedListings.TotalCount, request.PageNumber, request.PageSize);
    }
}
