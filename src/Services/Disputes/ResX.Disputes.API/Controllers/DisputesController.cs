using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResX.Disputes.Application.Commands.AddEvidence;
using ResX.Disputes.Application.Commands.CloseDispute;
using ResX.Disputes.Application.Commands.OpenDispute;
using ResX.Disputes.Application.Commands.ResolveDispute;
using ResX.Disputes.Application.DTOs;
using ResX.Disputes.Application.Queries.GetDispute;
using ResX.Disputes.Application.Repositories;

namespace ResX.Disputes.API.Controllers;

[ApiController]
[Route("api/disputes")]
[Authorize]
[Produces("application/json")]
public class DisputesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDisputeRepository _repository;

    public DisputesController(IMediator mediator, IDisputeRepository repository)
    {
        _mediator = mediator;
        _repository = repository;
    }

    /// <summary>
    /// Returns disputes visible to the caller. Admin/Moderator see every dispute regardless of status;
    /// other authenticated users see only disputes where they are initiator or respondent.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyDisputes(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var disputes = User.IsInRole("Admin") || User.IsInRole("Moderator")
            ? await _repository.GetAllAsync(pageNumber, pageSize, cancellationToken)
            : await _repository.GetByUserIdAsync(GetCurrentUserId(), pageNumber, pageSize, cancellationToken);

        return Ok(disputes.Select(d => new
        {
            d.Id,
            d.TransactionId,
            d.InitiatorId,
            d.RespondentId,
            d.Reason,
            Status = d.Status.ToString(),
            d.Resolution,
            d.CreatedAt,
            d.ResolvedAt
        }));
    }

    /// <summary>Returns a dispute by its ID including all evidence.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<DisputeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(new GetDisputeQuery(id), cancellationToken);
        return Ok(dto);
    }

    /// <summary>Returns all open and under-review disputes.</summary>
    [HttpGet("open")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOpenDisputes(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var disputes = await _repository.GetOpenDisputesAsync(pageNumber, pageSize, cancellationToken);

        return Ok(disputes);
    }

    /// <summary>Opens a new dispute for a transaction.</summary>
    [HttpPost]
    [ProducesResponseType<DisputeCreatedDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Open([FromBody] OpenDisputeRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var disputeId = await _mediator.Send(
            new OpenDisputeCommand(request.TransactionId, userId, request.RespondentId, request.Reason),
            cancellationToken);

        return Ok(new DisputeCreatedDto(disputeId));
    }

    /// <summary>Adds evidence (description and optional file URLs) to an open dispute.</summary>
    [HttpPost("{id:guid}/evidence")]
    [ProducesResponseType<EvidenceCreatedDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddEvidence(
        Guid id,
        [FromBody] AddEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var evidenceId = await _mediator.Send(
            new AddEvidenceCommand(id, userId, request.Description, request.FileUrls),
            cancellationToken);

        return Ok(new EvidenceCreatedDto(evidenceId));
    }

    /// <summary>Resolves a dispute with a written resolution.</summary>
    [HttpPost("{id:guid}/resolve")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resolve(
        Guid id,
        [FromBody] ResolveDisputeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _mediator.Send(new ResolveDisputeCommand(id, userId, request.Resolution), cancellationToken);

        return NoContent();
    }

    /// <summary>Closes a dispute without a formal resolution.</summary>
    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = "Moderator,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CloseDisputeCommand(id), cancellationToken);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }
}
