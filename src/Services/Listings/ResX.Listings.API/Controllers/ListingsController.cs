using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResX.Common.Models;
using ResX.Listings.Application.Commands.AddListingPhoto;
using ResX.Listings.Application.Commands.ChangeListingStatus;
using ResX.Listings.Application.Commands.CreateListing;
using ResX.Listings.Application.Commands.DeleteListing;
using ResX.Listings.Application.Commands.UpdateListing;
using ResX.Listings.Application.DTOs;
using ResX.Listings.Application.Queries.GetListingById;
using ResX.Listings.Application.Queries.GetListings;
using ResX.Listings.Application.Queries.GetMyListings;

namespace ResX.Listings.API.Controllers;

[ApiController]
[Route("api/listings")]
[Produces("application/json")]
public class ListingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ListingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns paginated listings with optional filters (category, condition, transfer type, city, search query).</summary>
    [HttpGet]
    [ProducesResponseType<PagedList<ListingPreviewDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetListings(
        [FromQuery] Guid? categoryId,
        [FromQuery] string? condition,
        [FromQuery] string? transferType,
        [FromQuery] string? city,
        [FromQuery] string? searchQuery,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetListingsQuery(categoryId, condition, transferType, city, searchQuery, pageNumber, pageSize),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Returns a listing by its ID and increments its view counter.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ListingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetListingByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns paginated listings created by the authenticated user.</summary>
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType<PagedList<ListingPreviewDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyListings(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var result = await _mediator.Send(
            new GetMyListingsQuery(userId, pageNumber, pageSize),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Creates a new listing for the authenticated donor.</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateListingDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var command = new CreateListingCommand(
            userId,
            dto.Title,
            dto.Description,
            dto.CategoryId,
            dto.WeightGrams,
            dto.Condition,
            dto.TransferType,
            dto.TransferMethod,
            dto.City,
            dto.District,
            dto.Latitude,
            dto.Longitude,
            dto.Tags);

        var listingId = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = listingId }, new { id = listingId });
    }

    /// <summary>Updates an existing listing. Only the listing owner can update it.</summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] CreateListingDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var command = new UpdateListingCommand(
            id, userId,
            dto.Title, dto.Description,
            dto.CategoryId,
            dto.WeightGrams,
            dto.Condition, dto.TransferType, dto.TransferMethod,
            dto.City, dto.District, dto.Latitude, dto.Longitude,
            dto.Tags);

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>Changes the status of a listing (e.g. Active → Reserved → Transferred). Only the listing owner can change status.</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeStatusRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _mediator.Send(new ChangeListingStatusCommand(id, userId, request.Status), cancellationToken);

        return NoContent();
    }

    /// <summary>Deletes a listing. Only the listing owner can delete it.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _mediator.Send(new DeleteListingCommand(id, userId), cancellationToken);

        return NoContent();
    }

    /// <summary>Adds a photo URL to an existing listing. Only the listing owner can add photos.</summary>
    [HttpPost("{id:guid}/photos")]
    [Authorize]
    [ProducesResponseType<PhotoCreatedDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddPhoto(
        Guid id,
        [FromBody] AddPhotoRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var photoId = await _mediator.Send(
            new AddListingPhotoCommand(id, userId, request.Url, request.DisplayOrder),
            cancellationToken);

        return Ok(new PhotoCreatedDto(photoId));
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }
}
