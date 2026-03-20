namespace ResX.Users.Application.DTOs;

public record ReviewDto(
    Guid Id,
    Guid ReviewerId,
    string ReviewerName,
    int Rating,
    string Comment,
    DateTime CreatedAt);