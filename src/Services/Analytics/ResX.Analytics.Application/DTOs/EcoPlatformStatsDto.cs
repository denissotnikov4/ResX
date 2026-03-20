namespace ResX.Analytics.Application.DTOs;

public record EcoPlatformStatsDto(
    long TotalItemsTransferred,
    decimal TotalCo2SavedKg,
    decimal TotalWasteSavedKg,
    int ActiveListings,
    int RegisteredUsers,
    DateTime LastUpdated);