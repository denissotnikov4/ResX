namespace ResX.Listings.Application.DTOs;

public record CategoryDetailsDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    string? IconUrl,
    bool IsActive,
    int DisplayOrder,
    int Co2SavedPer100GramsG,
    int WasteSavedPer100GramsG,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
