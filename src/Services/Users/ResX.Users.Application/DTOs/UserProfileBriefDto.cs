namespace ResX.Users.Application.DTOs;

public record UserProfileBriefDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    decimal Rating,
    int ReviewCount);