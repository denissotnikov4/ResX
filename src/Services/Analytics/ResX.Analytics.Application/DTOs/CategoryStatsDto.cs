namespace ResX.Analytics.Application.DTOs;

public record CategoryStatsDto(
    Guid CategoryId,
    string CategoryName,
    int ListingsCount,
    int TransactionsCount);