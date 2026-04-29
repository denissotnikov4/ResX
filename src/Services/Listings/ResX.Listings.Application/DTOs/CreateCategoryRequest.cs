namespace ResX.Listings.Application.DTOs;

public record CreateCategoryRequest(
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    string? IconUrl,
    int DisplayOrder);

public record UpdateCategoryRequest(
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    string? IconUrl,
    int DisplayOrder);
