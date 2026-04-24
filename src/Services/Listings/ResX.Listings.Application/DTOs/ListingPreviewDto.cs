namespace ResX.Listings.Application.DTOs;

public record ListingPreviewDto(
    Guid Id,
    string Title,
    CategoryDto Category,
    string Condition,
    string TransferType,
    string Status,
    string City,
    string? ThumbnailUrl,
    DonorDto? Donor,
    int ViewCount,
    DateTime CreatedAt);