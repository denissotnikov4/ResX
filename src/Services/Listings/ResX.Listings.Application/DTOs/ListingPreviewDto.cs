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
    int WeightGrams,
    int Co2SavedG,
    int WasteSavedG,
    DateTime CreatedAt);
