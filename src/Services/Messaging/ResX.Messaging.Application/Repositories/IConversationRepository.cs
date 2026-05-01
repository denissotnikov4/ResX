using ResX.Common.Models;
using ResX.Messaging.Domain.AggregateRoots;
using ResX.Messaging.Domain.Entities;

namespace ResX.Messaging.Application.Repositories;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Conversation?> GetByParticipantsAndListingAsync(
        Guid user1Id,
        Guid user2Id,
        Guid? listingId,
        CancellationToken cancellationToken = default);

    Task<PagedList<Conversation>> GetByUserIdAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedList<Message>> GetMessagesAsync(
        Guid conversationId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default);

    Task AppendMessageAsync(
        Guid conversationId,
        Message message,
        DateTime lastMessageAt,
        CancellationToken cancellationToken = default);

    Task<int> MarkMessagesAsReadAsync(
        Guid conversationId,
        Guid readerUserId,
        CancellationToken cancellationToken = default);
}
