using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Exceptions;
using ResX.Listings.Application.DTOs;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Domain.AggregateRoots;

namespace ResX.Listings.Application.Queries.GetListingById;

public class GetListingByIdQueryHandler : IRequestHandler<GetListingByIdQuery, ListingDto>
{
    private readonly IListingRepository _listingRepository;

    public GetListingByIdQueryHandler(
        IListingRepository listingRepository,
        ILogger<GetListingByIdQueryHandler> logger)
    {
        _listingRepository = listingRepository;
    }

    public async Task<ListingDto> Handle(GetListingByIdQuery request, CancellationToken cancellationToken)
    {
        var listing = await _listingRepository.GetByIdAsync(request.ListingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Listing), request.ListingId);

        await _listingRepository.IncrementViewCountAsync(listing.Id, cancellationToken);

        return MapToDto(listing);
    }

    private static ListingDto MapToDto(Listing listing) => new(
        listing.Id,
        listing.Title,
        listing.Description,
        new CategoryDto(listing.Category.Id, listing.Category.Name, listing.Category.ParentCategoryId),
        listing.Condition.ToString(),
        listing.TransferType.ToString(),
        listing.TransferMethod.ToString(),
        listing.Status.ToString(),
        new LocationDto(listing.Location.City, listing.Location.District, listing.Location.Latitude, listing.Location.Longitude),
        listing.DonorId,
        listing.Photos.Select(p => new ListingPhotoDto(p.Id, p.Url, p.DisplayOrder)).ToList().AsReadOnly(),
        listing.Tags.ToList().AsReadOnly(),
        listing.ViewCount,
        listing.CreatedAt,
        listing.UpdatedAt);
}
