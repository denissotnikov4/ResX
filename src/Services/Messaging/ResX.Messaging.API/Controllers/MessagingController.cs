using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResX.Messaging.Application.Commands.CreateConversation;
using ResX.Messaging.Application.Commands.MarkMessagesAsRead;
using ResX.Messaging.Application.Commands.SendMessage;
using ResX.Messaging.Application.DTOs;
using ResX.Messaging.Application.Queries.GetConversations;
using ResX.Messaging.Application.Queries.GetMessages;

namespace ResX.Messaging.API.Controllers;

[ApiController]
[Route("api/messaging")]
[Authorize]
[Produces("application/json")]
public class MessagingController : ControllerBase
{
    private readonly IMediator _mediator;

    public MessagingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var result = await _mediator.Send(
            new GetConversationsQuery(userId, pageNumber, pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var conversationId = await _mediator.Send(
            new CreateConversationCommand(userId, dto.RecipientId, dto.ListingId, dto.InitialMessage),
            cancellationToken);

        return Ok(new { conversationId });
    }

    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var result = await _mediator.Send(
            new GetMessagesQuery(conversationId, userId, pageNumber, pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid conversationId,
        [FromBody] SendMessageDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var message = await _mediator.Send(
            new SendMessageCommand(conversationId, userId, dto.Content),
            cancellationToken);

        return Ok(message);
    }

    [HttpPost("conversations/{conversationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid conversationId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _mediator.Send(new MarkMessagesAsReadCommand(conversationId, userId), cancellationToken);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }
}