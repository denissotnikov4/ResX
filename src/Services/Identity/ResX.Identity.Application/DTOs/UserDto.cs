using ResX.Identity.Domain.Enums;

namespace ResX.Identity.Application.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string? Phone,
    string FirstName,
    string LastName,
    UserRole Role,
    bool IsActive,
    DateTimeOffset CreatedAt);
