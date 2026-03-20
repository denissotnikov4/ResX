using MediatR;
using ResX.Common.Models;
using ResX.Listings.Application.DTOs;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Domain.Filters;

namespace ResX.Listings.Application.Queries.GetMyListings;

public class GetMyListingsQueryHandler : IRequestHandler<GetMyListingsQuery, PagedList<ListingPreviewDto>>
{
    private readonly IListingRepository _listingRepository;

    public GetMyListingsQueryHandler(IListingRepository listingRepository)
    {
        _listingRepository = listingRepository;
    }

    public async Task<PagedList<ListingPreviewDto>> Handle(GetMyListingsQuery request, CancellationToken cancellationToken)
    {
        var filter = new ListingFilter(DonorId: request.DonorId);

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

        return new PagedList<ListingPreviewDto>(listingPreviewDtos, pagedListings.TotalCount, request.PageNumber, request.PageSize);
    }
}
