using ResX.Common.Domain;
using ResX.Users.Domain.Entities;
using ResX.Users.Domain.ValueObjects;

namespace ResX.Users.Domain.Aggregates;

public class UserProfile : AggregateRoot<Guid>
{
    private readonly List<Review> _reviews = [];

    private UserProfile()
    {
    }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string? AvatarUrl { get; private set; }

    public string? Bio { get; private set; }

    public string? City { get; private set; }

    public decimal Rating { get; private set; }

    public int ReviewCount { get; private set; }

    public EcoStats EcoStats { get; private set; } = EcoStats.Create();

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<Review> Reviews => _reviews.AsReadOnly();

    public static UserProfile Create(
        Guid userId,
        string firstName,
        string lastName,
        string? city = null)
    {
        return new UserProfile
        {
            Id = userId,
            FirstName = firstName,
            LastName = lastName,
            City = city,
            Rating = 0,
            ReviewCount = 0,
            EcoStats = EcoStats.Create(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string firstName, string lastName, string? bio, string? city)
    {
        FirstName = firstName;
        LastName = lastName;
        Bio = bio;
        City = city;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAvatar(string avatarUrl)
    {
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public Review AddReview(Guid reviewerId, string reviewerName, int rating, string comment)
    {
        var review = Review.Create(Id, reviewerId, reviewerName, rating, comment);
        _reviews.Add(review);

        // Recalculate rating
        ReviewCount++;
        Rating = (Rating * (ReviewCount - 1) + rating) / ReviewCount;
        UpdatedAt = DateTime.UtcNow;

        return review;
    }

    public void UpdateEcoStats(int itemsGiftedDelta, int itemsReceivedDelta, decimal co2Delta, decimal wasteDelta)
    {
        EcoStats = EcoStats.Create(
            EcoStats.ItemsGifted + itemsGiftedDelta,
            EcoStats.ItemsReceived + itemsReceivedDelta,
            EcoStats.Co2SavedKg + co2Delta,
            EcoStats.WasteSavedKg + wasteDelta);
        UpdatedAt = DateTime.UtcNow;
    }
}