namespace ResX.Users.Application.DTOs;

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string? Bio,
    string? City);