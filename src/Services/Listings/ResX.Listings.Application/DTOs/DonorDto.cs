namespace ResX.Listings.Application.DTOs;

public record DonorDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    decimal Rating,
    int ReviewCount);
