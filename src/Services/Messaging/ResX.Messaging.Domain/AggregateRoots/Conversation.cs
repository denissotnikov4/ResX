using ResX.Common.Domain;
using ResX.Common.Exceptions;
using ResX.Messaging.Domain.Entities;

namespace ResX.Messaging.Domain.AggregateRoots;

public class Conversation : AggregateRoot<Guid>
{
    private readonly List<Message> _messages = [];
    private readonly List<Guid> _participants = [];

    private Conversation()
    {
    }

    public Guid? ListingId { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    
    public DateTime? LastMessageAt { get; private set; }

    public IReadOnlyCollection<Guid> Participants => _participants.AsReadOnly();
    
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    public static Conversation Create(IEnumerable<Guid> participants, Guid? listingId = null)
    {
        var participantList = participants.Distinct().ToList();
        if (participantList.Count < 2)
        {
            throw new DomainException("Conversation must have at least 2 participants.");
        }

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            CreatedAt = DateTime.UtcNow
        };

        conversation._participants.AddRange(participantList);
        
        return conversation;
    }

    public Message SendMessage(Guid senderId, string content)
    {
        if (!_participants.Contains(senderId))
        {
            throw new ForbiddenException("Only conversation participants can send messages.");
        }

        var message = Message.Create(Id, senderId, content);
        _messages.Add(message);
        LastMessageAt = DateTime.UtcNow;
        
        return message;
    }

    public bool HasParticipant(Guid userId)
    {
        return _participants.Contains(userId);
    }
}