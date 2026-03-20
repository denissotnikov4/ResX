namespace ResX.Users.Application.DTOs;

public record UserProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    string? Bio,
    string? City,
    decimal Rating,
    int ReviewCount,
    EcoStatsDto EcoStats,
    DateTime CreatedAt);
