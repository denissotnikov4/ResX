using MediatR;
using ResX.Analytics.Application.DTOs;

namespace ResX.Analytics.Application.Queries.GetCategoryStats;

public record GetCategoryStatsQuery : IRequest<IReadOnlyList<CategoryStatsDto>>;