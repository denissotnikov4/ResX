namespace ResX.Listings.Application.DTOs;

public record CategoryDetailsDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    string? IconUrl,
    bool IsActive,
    int DisplayOrder,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
