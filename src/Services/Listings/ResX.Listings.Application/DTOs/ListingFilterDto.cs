namespace ResX.Listings.Application.DTOs;

public record ListingFilterDto(
    Guid? CategoryId = null,
    string? Condition = null,
    string? TransferType = null,
    string? City = null,
    string? SearchQuery = null,
    int PageNumber = 1,
    int PageSize = 20);