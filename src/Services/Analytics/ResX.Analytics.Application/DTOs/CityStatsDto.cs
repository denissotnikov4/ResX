namespace ResX.Analytics.Application.DTOs;

public record CityStatsDto(
    string City,
    int ListingsCount,
    int UsersCount,
    decimal Co2SavedKg);