using MediatR;

namespace ResX.Messaging.Application.Commands.MarkMessagesAsRead;

public record MarkMessagesAsReadCommand(
    Guid ConversationId,
    Guid UserId) : IRequest<Unit>;