using MediatR;
using ResX.Messaging.Application.DTOs;

namespace ResX.Messaging.Application.Commands.SendMessage;

public record SendMessageCommand(
    Guid ConversationId,
    Guid SenderId,
    string Content) : IRequest<MessageDto>;