using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResX.Common.Models;
using ResX.Users.Application.Commands.AddReview;
using ResX.Users.Application.Commands.UpdateAvatar;
using ResX.Users.Application.Commands.UpdateUserProfile;
using ResX.Users.Application.DTOs;
using ResX.Users.Application.Queries.GetEcoLeaderboard;
using ResX.Users.Application.Queries.GetUserProfile;
using ResX.Users.Application.Queries.GetUserReviews;

namespace ResX.Users.API.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserProfileQuery(id), cancellationToken);

        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result = await _mediator.Send(new GetUserProfileQuery(userId), cancellationToken);

        return Ok(result);
    }

    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _mediator.Send(
            new UpdateUserProfileCommand(userId, request.FirstName, request.LastName, request.Bio, request.City),
            cancellationToken);

        return NoContent();
    }

    [HttpPut("me/avatar")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateAvatar(
        [FromBody] UpdateAvatarRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _mediator.Send(new UpdateAvatarCommand(userId, request.AvatarUrl), cancellationToken);

        return NoContent();
    }

    [HttpGet("{id:guid}/reviews")]
    [ProducesResponseType<PagedList<ReviewDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReviews(
        Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetUserReviewsQuery(id, pageNumber, pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/reviews")]
    [Authorize]
    [ProducesResponseType<ReviewCreatedDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddReview(
        Guid id,
        [FromBody] AddReviewRequest request,
        CancellationToken cancellationToken)
    {
        var reviewerId = GetCurrentUserId();
        if (reviewerId == id)
        {
            return BadRequest("You cannot review yourself.");
        }

        var reviewId = await _mediator.Send(
            new AddReviewCommand(id, reviewerId, request.ReviewerName, request.Rating, request.Comment),
            cancellationToken);

        return Ok(new ReviewCreatedDto(reviewId));
    }

    [HttpGet("leaderboard")]
    [ProducesResponseType<PagedList<UserProfileDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEcoLeaderboard(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetEcoLeaderboardQuery(pageNumber, pageSize),
            cancellationToken);

        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }
}