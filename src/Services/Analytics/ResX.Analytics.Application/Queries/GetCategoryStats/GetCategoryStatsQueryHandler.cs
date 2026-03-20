using MediatR;
using ResX.Analytics.Application.DTOs;
using ResX.Analytics.Application.Repositories;

namespace ResX.Analytics.Application.Queries.GetCategoryStats;

public class GetCategoryStatsQueryHandler : IRequestHandler<GetCategoryStatsQuery, IReadOnlyList<CategoryStatsDto>>
{
    private readonly IAnalyticsRepository _repository;

    public GetCategoryStatsQueryHandler(IAnalyticsRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CategoryStatsDto>> Handle(
        GetCategoryStatsQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.GetCategoryStatsAsync(cancellationToken);
    }
}