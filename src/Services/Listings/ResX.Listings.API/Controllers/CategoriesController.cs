using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResX.Listings.Application.Commands.CreateCategory;
using ResX.Listings.Application.Commands.DeactivateCategory;
using ResX.Listings.Application.Commands.UpdateCategory;
using ResX.Listings.Application.DTOs;
using ResX.Listings.Application.Queries.GetCategories;
using ResX.Listings.Application.Queries.GetCategoryHistory;

namespace ResX.Listings.API.Controllers;

[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns all active listing categories ordered by display order. Public endpoint.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryDetailsDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var categories = await _mediator.Send(new GetCategoriesQuery(), cancellationToken);
        return Ok(categories);
    }

    /// <summary>Creates a new category. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var command = new CreateCategoryCommand(
            userId,
            request.Name,
            request.Description,
            request.ParentCategoryId,
            request.IconUrl,
            request.DisplayOrder);

        var categoryId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = categoryId }, new { id = categoryId });
    }

    /// <summary>Updates an existing category. Admin only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var command = new UpdateCategoryCommand(
            id,
            userId,
            request.Name,
            request.Description,
            request.ParentCategoryId,
            request.IconUrl,
            request.DisplayOrder);

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Deactivates (soft-deletes) a category. Admin only. Existing listings keep their CategoryId reference.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _mediator.Send(new DeactivateCategoryCommand(id, userId), cancellationToken);
        return NoContent();
    }

    /// <summary>Returns the full change history for a category, newest first. Admin only.</summary>
    [HttpGet("{id:guid}/history")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<IReadOnlyList<CategoryHistoryEntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var entries = await _mediator.Send(new GetCategoryHistoryQuery(id), cancellationToken);
        return Ok(entries);
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }
}
