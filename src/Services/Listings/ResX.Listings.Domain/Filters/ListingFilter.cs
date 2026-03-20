namespace ResX.Listings.Domain.Filters;

public record ListingFilter(
    Guid? CategoryId = null,
    string? Condition = null,
    string? TransferType = null,
    string? City = null,
    Guid? DonorId = null,
    string? SearchQuery = null);