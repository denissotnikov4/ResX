using MediatR;
using ResX.Common.Models;
using ResX.Messaging.Application.DTOs;
using ResX.Messaging.Application.Repositories;
using ResX.Messaging.Application.Services;

namespace ResX.Messaging.Application.Queries.GetConversations;

public class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, PagedList<ConversationDto>>
{
    private readonly IConversationRepository _repository;
    private readonly IUsersClient _usersClient;
    private readonly IListingsClient _listingsClient;

    public GetConversationsQueryHandler(
        IConversationRepository repository,
        IUsersClient usersClient,
        IListingsClient listingsClient)
    {
        _repository = repository;
        _usersClient = usersClient;
        _listingsClient = listingsClient;
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

        if (conversations.Items.Count == 0)
        {
            return new PagedList<ConversationDto>(
                Array.Empty<ConversationDto>(),
                conversations.TotalCount,
                request.PageNumber,
                request.PageSize);
        }

        var participantIds = conversations.Items
            .SelectMany(c => c.Participants)
            .Distinct()
            .ToList();

        var listingIds = conversations.Items
            .Where(c => c.ListingId.HasValue)
            .Select(c => c.ListingId!.Value)
            .Distinct()
            .ToList();

        var usersTask = _usersClient.GetUserSummariesAsync(participantIds, cancellationToken);
        var listingsTask = listingIds.Count > 0
            ? _listingsClient.GetListingSummariesAsync(listingIds, cancellationToken)
            : Task.FromResult<IReadOnlyDictionary<Guid, ListingSummaryDto>>(
                new Dictionary<Guid, ListingSummaryDto>());

        await Task.WhenAll(usersTask, listingsTask);
        var users = usersTask.Result;
        var listings = listingsTask.Result;

        var conversationDtos = conversations.Items.Select(c =>
        {
            var lastMsg = c.Messages.MaxBy(m => m.SentAt);
            var unreadCount = c.Messages.Count(m => m.SenderId != request.UserId && !m.IsRead);

            var participantSummaries = c.Participants
                .Select(pid => users.TryGetValue(pid, out var u)
                    ? u
                    : new ParticipantSummaryDto(pid, "(deleted)", "user", null))
                .ToList()
                .AsReadOnly();

            // For 1-on-1 chats — the other side; for group — first non-self participant.
            var counterpartyId = c.Participants.FirstOrDefault(p => p != request.UserId);
            ParticipantSummaryDto? counterparty = counterpartyId != Guid.Empty
                && users.TryGetValue(counterpartyId, out var cp) ? cp : null;

            ListingSummaryDto? listingSummary = null;
            if (c.ListingId.HasValue)
            {
                listingSummary = listings.TryGetValue(c.ListingId.Value, out var l)
                    ? l
                    : new ListingSummaryDto(c.ListingId.Value, "(removed)");
            }

            return new ConversationDto(
                c.Id,
                participantSummaries,
                counterparty,
                c.ListingId,
                listingSummary,
                lastMsg != null
                    ? new MessageDto(
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
