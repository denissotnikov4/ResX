namespace ResX.Listings.Application.DTOs;

public record CategoryDto(
    Guid Id,
    string Name,
    Guid? ParentCategoryId);