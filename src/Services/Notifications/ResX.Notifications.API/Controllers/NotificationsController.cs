using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ResX.Notifications.Application.Commands.MarkAllNotificationsAsRead;
using ResX.Notifications.Application.Commands.MarkNotificationAsRead;
using ResX.Notifications.Application.Queries.GetMyNotifications;

namespace ResX.Notifications.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Возвращает список уведомлений текущего пользователя с количеством непрочитанных уведомлений
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool onlyUnread = false,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var result = await _mediator.Send(
            new GetMyNotificationsQuery(userId, pageNumber, pageSize, onlyUnread),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Помечает конкретное уведомление как прочитанное
    /// </summary>
    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _mediator.Send(new MarkNotificationAsReadCommand(id, userId), cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Помечает все уведомления текущего пользователя как прочитанные
    /// </summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _mediator.Send(new MarkAllNotificationsAsReadCommand(userId), cancellationToken);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }
}
