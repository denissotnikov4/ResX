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
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Conversation?> GetByParticipantsAndListingAsync(
        Guid user1Id, Guid user2Id, Guid? listingId,
        CancellationToken cancellationToken = default)
    {
        var conversations = await _context.Conversations
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

    public async Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        // Collect new messages before touching the tracker (safe — no DetectChanges triggered)
        var trackedIds = _context.ChangeTracker.Entries<Message>()
            .Select(e => e.Entity.Id).ToHashSet();

        var newMessages = conversation.Messages
            .Where(m => !trackedIds.Contains(m.Id))
            .ToList();

        // Detach ALL tracked entities to avoid spurious UPDATEs from snapshot false-positives
        // (e.g. Participants uses AsReadOnly() → new wrapper instance each read → always "modified")
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
            entry.State = EntityState.Detached;

        // Register new messages — IUnitOfWork.SaveChangesAsync() (called by the handler) INSERTs them
        foreach (var msg in newMessages) _context.Messages.Add(msg);

        // Update conversation metadata via raw SQL — direct commit, bypasses change tracking
        await _context.Conversations
            .Where(c => c.Id == conversation.Id)
            .ExecuteUpdateAsync(
                s => s.SetProperty(c => c.LastMessageAt, conversation.LastMessageAt),
                cancellationToken);
    }
}