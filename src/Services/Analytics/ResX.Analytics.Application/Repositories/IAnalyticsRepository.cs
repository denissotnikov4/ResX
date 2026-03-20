using ResX.Analytics.Application.DTOs;

namespace ResX.Analytics.Application.Repositories;

public interface IAnalyticsRepository
{
    Task<EcoPlatformStatsDto> GetEcoStatsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryStatsDto>> GetCategoryStatsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CityStatsDto>> GetCityStatsAsync(CancellationToken cancellationToken = default);
}