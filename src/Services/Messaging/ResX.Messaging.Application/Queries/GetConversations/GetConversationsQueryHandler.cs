using MediatR;
using ResX.Common.Models;
using ResX.Messaging.Application.DTOs;
using ResX.Messaging.Application.Repositories;

namespace ResX.Messaging.Application.Queries.GetConversations;

public class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, PagedList<ConversationDto>>
{
    private readonly IConversationRepository _repository;

    public GetConversationsQueryHandler(IConversationRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedList<ConversationDto>> Handle(
        GetConversationsQuery request, 
        CancellationToken cancellationToken)
    {
        var conversations = await _repository.GetByUserIdAsync(
            request.UserId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var conversationDtos = conversations.Items.Select(c =>
        {
            var lastMsg = c.Messages.MaxBy(m => m.SentAt);
            var unreadCount = c.Messages.Count(m => m.SenderId != request.UserId && !m.IsRead);
            return new ConversationDto(
                c.Id,
                c.Participants.ToList().AsReadOnly(),
                c.ListingId,
                lastMsg != null ? 
                    new MessageDto(
                        lastMsg.Id,
                        lastMsg.ConversationId,
                        lastMsg.SenderId,
                        lastMsg.Content,
                        lastMsg.SentAt,
                        lastMsg.IsRead) 
                    : null,
                unreadCount,
                c.CreatedAt,
                c.LastMessageAt);
        }).ToList().AsReadOnly();

        return new PagedList<ConversationDto>(
            conversationDtos,
            conversations.TotalCount,
            request.PageNumber,
            request.PageSize);
    }
}