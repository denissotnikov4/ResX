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
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUsersClient _usersClient;

    public GetListingByIdQueryHandler(
        IListingRepository listingRepository,
        ICategoryRepository categoryRepository,
        IUsersClient usersClient,
        ILogger<GetListingByIdQueryHandler> logger)
    {
        _listingRepository = listingRepository;
        _categoryRepository = categoryRepository;
        _usersClient = usersClient;
    }

    public async Task<ListingDto> Handle(GetListingByIdQuery request, CancellationToken cancellationToken)
    {
        var listing = await _listingRepository.GetByIdAsync(request.ListingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Listing), request.ListingId);

        await _listingRepository.IncrementViewCountAsync(listing.Id, cancellationToken);

        var donorsTask = _usersClient.GetDonorsAsync(new[] { listing.DonorId }, cancellationToken);
        var categoryTask = _categoryRepository.GetByIdAsync(listing.CategoryId, cancellationToken);
        await Task.WhenAll(donorsTask, categoryTask);

        var donors = donorsTask.Result;
        var category = categoryTask.Result;

        if (!donors.TryGetValue(listing.DonorId, out var donor))
            throw new NotFoundException("Donor", listing.DonorId);

        var categoryDto = category is null
            ? new CategoryDto(listing.CategoryId, "(deleted)", null)
            : new CategoryDto(category.Id, category.Name, category.ParentCategoryId);

        return new ListingDto(
            listing.Id,
            listing.Title,
            listing.Description,
            categoryDto,
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
