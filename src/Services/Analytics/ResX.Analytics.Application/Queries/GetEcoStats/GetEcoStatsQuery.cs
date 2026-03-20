using MediatR;
using ResX.Analytics.Application.DTOs;

namespace ResX.Analytics.Application.Queries.GetEcoStats;

public record GetEcoStatsQuery : IRequest<EcoPlatformStatsDto>;