using MediatR;
using ResX.Analytics.Application.DTOs;
using ResX.Analytics.Application.Repositories;

namespace ResX.Analytics.Application.Queries.GetCityStats;

public class GetCityStatsQueryHandler : IRequestHandler<GetCityStatsQuery, IReadOnlyList<CityStatsDto>>
{
    private readonly IAnalyticsRepository _repository;

    public GetCityStatsQueryHandler(IAnalyticsRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CityStatsDto>> Handle(
        GetCityStatsQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.GetCityStatsAsync(cancellationToken);
    }
}