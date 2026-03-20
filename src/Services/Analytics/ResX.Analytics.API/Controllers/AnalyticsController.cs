using MediatR;
using Microsoft.AspNetCore.Mvc;
using ResX.Analytics.Application.DTOs;
using ResX.Analytics.Application.Queries.GetCategoryStats;
using ResX.Analytics.Application.Queries.GetCityStats;
using ResX.Analytics.Application.Queries.GetEcoStats;

namespace ResX.Analytics.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalyticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Общая экологическая статистика платформы
    /// </summary>
    [HttpGet("eco-stats")]
    [ProducesResponseType<EcoPlatformStatsDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEcoStats(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEcoStatsQuery(), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Статистика по категориям вещей
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType<IReadOnlyList<CategoryStatsDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryStats(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCategoryStatsQuery(), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Статистика по городам
    /// </summary>
    [HttpGet("cities")]
    [ProducesResponseType<IReadOnlyList<CityStatsDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCityStats(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCityStatsQuery(), cancellationToken);

        return Ok(result);
    }
}