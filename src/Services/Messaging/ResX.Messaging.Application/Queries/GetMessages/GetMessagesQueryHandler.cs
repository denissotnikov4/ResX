using MediatR;
using ResX.Common.Exceptions;
using ResX.Common.Models;
using ResX.Messaging.Application.DTOs;
using ResX.Messaging.Application.Repositories;
using ResX.Messaging.Domain.AggregateRoots;

namespace ResX.Messaging.Application.Queries.GetMessages;

public class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, PagedList<MessageDto>>
{
    private readonly IConversationRepository _repository;

    public GetMessagesQueryHandler(IConversationRepository repository) => _repository = repository;

    public async Task<PagedList<MessageDto>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetByIdAsync(request.ConversationId, cancellationToken)
                           ?? throw new NotFoundException(nameof(Conversation), request.ConversationId);

        if (!conversation.HasParticipant(request.UserId))
        {
            throw new ForbiddenException("You are not a participant in this conversation.");
        }

        var messages = await _repository.GetMessagesAsync(
            request.ConversationId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
        
        var messageDtos = messages.Items
            .Select(m => new MessageDto(m.Id, m.ConversationId, m.SenderId, m.Content, m.SentAt, m.IsRead))
            .ToList().AsReadOnly();

        return new PagedList<MessageDto>(messageDtos, messages.TotalCount, request.PageNumber, request.PageSize);
    }
}