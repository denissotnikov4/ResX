namespace ResX.Listings.Application.DTOs;

public record ListingDto(
    Guid Id,
    string Title,
    string Description,
    CategoryDto Category,
    string Condition,
    string TransferType,
    string TransferMethod,
    string Status,
    LocationDto Location,
    DonorDto Donor,
    IReadOnlyList<ListingPhotoDto> Photos,
    IReadOnlyList<string> Tags,
    int ViewCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);