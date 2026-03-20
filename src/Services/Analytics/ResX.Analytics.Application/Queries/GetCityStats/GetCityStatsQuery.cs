using MediatR;
using ResX.Analytics.Application.DTOs;

namespace ResX.Analytics.Application.Queries.GetCityStats;

public record GetCityStatsQuery : IRequest<IReadOnlyList<CityStatsDto>>;