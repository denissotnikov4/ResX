using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Exceptions;
using ResX.Listings.Application.DTOs;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Application.Services;
using ResX.Listings.Domain.AggregateRoots;

namespace ResX.Listings.Application.Queries.GetListingById;

public class GetListingByIdQueryHandler : IRequestHandler<GetListingByIdQuery, ListingDto>
{
    private readonly IListingRepository _listingRepository;
    private readonly IUsersClient _usersClient;

    public GetListingByIdQueryHandler(
        IListingRepository listingRepository,
        IUsersClient usersClient,
        ILogger<GetListingByIdQueryHandler> logger)
    {
        _listingRepository = listingRepository;
        _usersClient = usersClient;
    }

    public async Task<ListingDto> Handle(GetListingByIdQuery request, CancellationToken cancellationToken)
    {
        var listing = await _listingRepository.GetByIdAsync(request.ListingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Listing), request.ListingId);

        await _listingRepository.IncrementViewCountAsync(listing.Id, cancellationToken);

        var donors = await _usersClient.GetDonorsAsync(new[] { listing.DonorId }, cancellationToken);
        if (!donors.TryGetValue(listing.DonorId, out var donor))
            throw new NotFoundException("Donor", listing.DonorId);

        return new ListingDto(
            listing.Id,
            listing.Title,
            listing.Description,
            new CategoryDto(listing.Category.Id, listing.Category.Name, listing.Category.ParentCategoryId),
            listing.Condition.ToString(),
            listing.TransferType.ToString(),
            listing.TransferMethod.ToString(),
            listing.Status.ToString(),
            new LocationDto(listing.Location.City, listing.Location.District, listing.Location.Latitude, listing.Location.Longitude),
            donor,
            listing.Photos.Select(p => new ListingPhotoDto(p.Id, p.Url, p.DisplayOrder)).ToList().AsReadOnly(),
            listing.Tags.ToList().AsReadOnly(),
            listing.ViewCount,
            listing.CreatedAt,
            listing.UpdatedAt);
    }
}
