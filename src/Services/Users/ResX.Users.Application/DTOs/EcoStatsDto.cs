namespace ResX.Users.Application.DTOs;

public record EcoStatsDto(
    int ItemsGifted,
    int ItemsReceived,
    decimal Co2SavedKg,
    decimal WasteSavedKg);