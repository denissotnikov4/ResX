using MediatR;
using ResX.Common.Exceptions;
using ResX.Messaging.Application.Repositories;
using ResX.Messaging.Domain.AggregateRoots;

namespace ResX.Messaging.Application.Commands.MarkMessagesAsRead;

public class MarkMessagesAsReadCommandHandler : IRequestHandler<MarkMessagesAsReadCommand, Unit>
{
    private readonly IConversationRepository _repository;

    public MarkMessagesAsReadCommandHandler(IConversationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(MarkMessagesAsReadCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetByIdAsync(request.ConversationId, cancellationToken)
                           ?? throw new NotFoundException(nameof(Conversation), request.ConversationId);

        if (!conversation.HasParticipant(request.UserId))
        {
            throw new ForbiddenException("You are not a participant in this conversation.");
        }

        await _repository.MarkMessagesAsReadAsync(request.ConversationId, request.UserId, cancellationToken);

        return Unit.Value;
    }
}
