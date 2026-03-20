namespace ResX.Listings.Application.DTOs;

public record ListingPhotoDto(
    Guid Id,
    string Url,
    int DisplayOrder);