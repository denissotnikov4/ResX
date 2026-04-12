using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResX.Identity.Application.Commands.ChangePassword;
using ResX.Identity.Application.Commands.LoginUser;
using ResX.Identity.Application.Commands.Logout;
using ResX.Identity.Application.Commands.RefreshToken;
using ResX.Identity.Application.Commands.RegisterUser;
using ResX.Identity.Application.DTOs;
using ResX.Identity.Application.Services;

namespace ResX.Identity.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITokenService _tokenService;

    public AuthController(IMediator mediator, ITokenService tokenService)
    {
        _mediator = mediator;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        var tokens = await _mediator.Send(command, cancellationToken);

        SetTokenCookies(tokens);

        return Created();
    }

    /// <summary>
    /// Login with email/phone and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        var tokens = await _mediator.Send(command, cancellationToken);

        SetTokenCookies(tokens);

        return NoContent();
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized();
        }

        var tokens = await _mediator.Send(new RefreshTokenCommand(refreshToken), cancellationToken);

        SetTokenCookies(tokens);

        return NoContent();
    }

    /// <summary>
    /// Logout (revoke refresh token)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return NoContent();
        }

        await _mediator.Send(new LogoutCommand(refreshToken), cancellationToken);

        ClearTokenCookies();

        return NoContent();
    }

    /// <summary>
    /// Change password
    /// </summary>
    [HttpPut("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var command = new ChangePasswordCommand(userId, request.OldPassword, request.NewPassword);
        await _mediator.Send(command, cancellationToken);

        ClearTokenCookies();

        return NoContent();
    }

    private void SetTokenCookies(TokensDto tokens)
    {
        Response.Cookies.Append("accessToken", tokens.AccessToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromMinutes(_tokenService.GetAccessTokenExpiryMinutes())
        });

        Response.Cookies.Append("refreshToken", tokens.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(30)
        });
    }

    private void ClearTokenCookies()
    {
        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");
    }
}