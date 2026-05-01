using Microsoft.EntityFrameworkCore;
using ResX.Common.Models;
using ResX.Messaging.Application.Repositories;
using ResX.Messaging.Domain.AggregateRoots;
using ResX.Messaging.Domain.Entities;

namespace ResX.Messaging.Infrastructure.Persistence.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly MessagingDbContext _context;

    public ConversationRepository(MessagingDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .AsNoTracking()
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Conversation?> GetByParticipantsAndListingAsync(
        Guid user1Id, Guid user2Id, Guid? listingId,
        CancellationToken cancellationToken = default)
    {
        var conversations = await _context.Conversations
            .AsNoTracking()
            .Include(c => c.Messages)
            .Where(c => c.ListingId == listingId)
            .ToListAsync(cancellationToken);

        return conversations.FirstOrDefault(c =>
            c.Participants.Contains(user1Id) && c.Participants.Contains(user2Id));
    }

    public async Task<PagedList<Conversation>> GetByUserIdAsync(
        Guid userId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var all = await _context.Conversations
            .AsNoTracking()
            .Include(c => c.Messages)
            .ToListAsync(cancellationToken);

        var filtered = all
            .Where(c => c.Participants.Contains(userId))
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToList();

        var totalCount = filtered.Count;
        var items = filtered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedList<Conversation>(items.AsReadOnly(), totalCount, pageNumber, pageSize);
    }

    public async Task<PagedList<Message>> GetMessagesAsync(
        Guid conversationId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<Message>(items.AsReadOnly(), totalCount, pageNumber, pageSize);
    }

    public Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        _context.Conversations.Add(conversation);
        return Task.CompletedTask;
    }

    public async Task AppendMessageAsync(
        Guid conversationId,
        Message message,
        DateTime lastMessageAt,
        CancellationToken cancellationToken = default)
    {
        // Two explicit operations; keep change-tracker out of the picture entirely.
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        await _context.Conversations
            .Where(c => c.Id == conversationId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(c => c.LastMessageAt, lastMessageAt),
                cancellationToken);
    }

    public Task<int> MarkMessagesAsReadAsync(
        Guid conversationId,
        Guid readerUserId,
        CancellationToken cancellationToken = default)
    {
        return _context.Messages
            .Where(m => m.ConversationId == conversationId
                        && m.SenderId != readerUserId
                        && !m.IsRead)
            .ExecuteUpdateAsync(
                s => s.SetProperty(m => m.IsRead, true),
                cancellationToken);
    }
}
