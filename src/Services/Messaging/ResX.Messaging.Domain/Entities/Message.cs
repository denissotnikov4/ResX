using ResX.Common.Domain;
using ResX.Common.Exceptions;

namespace ResX.Messaging.Domain.Entities;

public class Message : Entity<Guid>
{
    private Message()
    {
    }

    public Guid ConversationId { get; private set; }

    public Guid SenderId { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public DateTime SentAt { get; private set; }

    public bool IsRead { get; private set; }

    public static Message Create(Guid conversationId, Guid senderId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainException("Message content cannot be empty.");
        }

        if (content.Length > 2000)
        {
            throw new DomainException("Message content cannot exceed 2000 characters.");
        }

        return new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}