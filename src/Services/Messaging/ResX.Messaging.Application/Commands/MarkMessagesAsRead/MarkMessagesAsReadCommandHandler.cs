using MediatR;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Messaging.Application.Repositories;
using ResX.Messaging.Domain.AggregateRoots;

namespace ResX.Messaging.Application.Commands.MarkMessagesAsRead;

public class MarkMessagesAsReadCommandHandler : IRequestHandler<MarkMessagesAsReadCommand, Unit>
{
    private readonly IConversationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkMessagesAsReadCommandHandler(IConversationRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(MarkMessagesAsReadCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetByIdAsync(request.ConversationId, cancellationToken)
                           ?? throw new NotFoundException(nameof(Conversation), request.ConversationId);

        if (!conversation.HasParticipant(request.UserId))
        {
            throw new ForbiddenException("You are not a participant in this conversation.");
        }

        foreach (var message in conversation.Messages.Where(m => m.SenderId != request.UserId && !m.IsRead))
        {
            message.MarkAsRead();
        }

        await _repository.UpdateAsync(conversation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
