using ResX.Common.Domain;
using ResX.Common.Exceptions;

namespace ResX.Users.Domain.Entities;

public class Review : Entity<Guid>
{
    private Review()
    {
    }

    public Guid ReviewerId { get; private set; }

    public string ReviewerName { get; private set; } = string.Empty;

    public int Rating { get; private set; }

    public string Comment { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public Guid UserProfileId { get; private set; }

    public static Review Create(
        Guid userProfileId,
        Guid reviewerId,
        string reviewerName,
        int rating,
        string comment)
    {
        if (rating is < 1 or > 5)
        {
            throw new DomainException("Rating must be between 1 and 5.");
        }

        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new DomainException("Comment cannot be empty.");
        }

        return new Review
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            ReviewerId = reviewerId,
            ReviewerName = reviewerName,
            Rating = rating,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        };
    }
}