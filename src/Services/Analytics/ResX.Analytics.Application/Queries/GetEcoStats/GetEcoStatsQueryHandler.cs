using MediatR;
using ResX.Analytics.Application.DTOs;
using ResX.Analytics.Application.Repositories;

namespace ResX.Analytics.Application.Queries.GetEcoStats;

public class GetEcoStatsQueryHandler : IRequestHandler<GetEcoStatsQuery, EcoPlatformStatsDto>
{
    private readonly IAnalyticsRepository _repository;

    public GetEcoStatsQueryHandler(IAnalyticsRepository repository)
    {
        _repository = repository;
    }

    public async Task<EcoPlatformStatsDto> Handle(GetEcoStatsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetEcoStatsAsync(cancellationToken);
    }
}