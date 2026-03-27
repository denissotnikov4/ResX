using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResX.Charity.Application.Commands.CancelCharityRequest;
using ResX.Charity.Application.Commands.CompleteCharityRequest;
using ResX.Charity.Application.Commands.CreateCharityRequest;
using ResX.Charity.Application.Commands.CreateOrganization;
using ResX.Charity.Application.Commands.RejectOrganization;
using ResX.Charity.Application.Commands.VerifyOrganization;
using ResX.Charity.Application.DTOs;
using ResX.Charity.Application.Queries.GetCharityRequest;
using ResX.Charity.Application.Queries.GetOrganization;
using ResX.Charity.Application.Repositories;
using ResX.Charity.Domain.AggregateRoots;
using ResX.Common.Models;

namespace ResX.Charity.API.Controllers;

[ApiController]
[Route("api/charity")]
[Produces("application/json")]
public class CharityController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IOrganizationRepository _orgRepository;
    private readonly ICharityRequestRepository _requestRepository;

    public CharityController(
        IMediator mediator,
        ICharityRequestRepository requestRepository,
        IOrganizationRepository orgRepository)
    {
        _mediator = mediator;
        _requestRepository = requestRepository;
        _orgRepository = orgRepository;
    }

    /// <summary>Returns paginated list of all active charity requests.</summary>
    [HttpGet("requests")]
    [ProducesResponseType<PagedList<CharityRequestDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCharityRequests(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var items = await _requestRepository.GetActiveAsync(pageNumber, pageSize, cancellationToken);
        var total = await _requestRepository.GetActiveTotalCountAsync(cancellationToken);

        var dtos = items.Select(MapToDto).ToList().AsReadOnly();

        return Ok(new PagedList<CharityRequestDto>(dtos, total, pageNumber, pageSize));
    }

    /// <summary>Returns a single charity request by its ID. Public endpoint.</summary>
    [HttpGet("requests/{id:guid}")]
    [ProducesResponseType<CharityRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCharityRequest(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(new GetCharityRequestQuery(id), cancellationToken);
        return Ok(dto);
    }

    /// <summary>Returns an organization by its ID. Public endpoint.</summary>
    [HttpGet("organizations/{id:guid}")]
    [ProducesResponseType<OrganizationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganization(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(new GetOrganizationQuery(id), cancellationToken);
        return Ok(dto);
    }

    /// <summary>Creates a new charity organization for the authenticated user.</summary>
    [HttpPost("organizations")]
    [Authorize]
    [ProducesResponseType<OrganizationCreatedDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateOrganization(
        [FromBody] CreateOrganizationDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var orgId = await _mediator.Send(
            new CreateOrganizationCommand(userId, dto.Name, dto.Description, dto.LegalDocumentUrl),
            cancellationToken);

        return Ok(new OrganizationCreatedDto(orgId));
    }

    /// <summary>Verifies an organization, allowing it to post charity requests.</summary>
    [HttpPut("organizations/{id:guid}/verify")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyOrganization(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new VerifyOrganizationCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Rejects an organization's verification.</summary>
    [HttpPut("organizations/{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectOrganization(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RejectOrganizationCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Creates a charity request for the current user's verified organization.</summary>
    [HttpPost("requests")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateCharityRequest(
        [FromBody] CreateCharityRequestDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var organization = await _orgRepository.GetByUserIdAsync(userId, cancellationToken);
        if (organization == null)
        {
            return BadRequest("Organization not found for this user.");
        }

        var requestId = await _mediator.Send(
            new CreateCharityRequestCommand(organization.Id, dto.Title, dto.Description, dto.DeadlineDate, dto.Items),
            cancellationToken);

        return CreatedAtAction(nameof(GetCharityRequest), new { id = requestId }, new { id = requestId });
    }

    /// <summary>Cancels a charity request.</summary>
    [HttpPost("requests/{id:guid}/cancel")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelCharityRequest(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelCharityRequestCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Marks a charity request as completed.</summary>
    [HttpPost("requests/{id:guid}/complete")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteCharityRequest(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CompleteCharityRequestCommand(id), cancellationToken);
        return NoContent();
    }

    private static CharityRequestDto MapToDto(CharityRequest r)
    {
        return new CharityRequestDto(
            r.Id,
            r.OrganizationId,
            r.Title,
            r.Description,
            r.Status.ToString(),
            r.RequestedItems
                .Select(i =>
                    new RequestedItemDto(
                        i.Id,
                        i.CategoryId,
                        i.CategoryName,
                        i.QuantityNeeded,
                        i.QuantityReceived,
                        i.Condition))
                .ToList()
                .AsReadOnly(),
            r.DeadlineDate,
            r.CreatedAt);
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }
}
