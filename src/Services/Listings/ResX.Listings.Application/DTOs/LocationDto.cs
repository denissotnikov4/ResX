namespace ResX.Listings.Application.DTOs;

public record LocationDto(
    string City,
    string? District,
    double? Latitude,
    double? Longitude);