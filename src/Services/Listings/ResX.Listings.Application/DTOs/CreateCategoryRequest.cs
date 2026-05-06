namespace ResX.Listings.Application.DTOs;

public record CreateCategoryRequest(
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    string? IconUrl,
    int DisplayOrder,
    int Co2SavedPer100GramsG,
    int WasteSavedPer100GramsG);

public record UpdateCategoryRequest(
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    string? IconUrl,
    int DisplayOrder,
    int Co2SavedPer100GramsG,
    int WasteSavedPer100GramsG);
