namespace ResX.Listings.Application.DTOs;

public record CategoryResultDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int DisplayOrder);
