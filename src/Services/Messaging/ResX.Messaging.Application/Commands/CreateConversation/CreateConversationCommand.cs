using MediatR;

namespace ResX.Messaging.Application.Commands.CreateConversation;

public record CreateConversationCommand(
    Guid InitiatorId,
    Guid RecipientId,
    Guid? ListingId,
    string? InitialMessage) : IRequest<Guid>;
