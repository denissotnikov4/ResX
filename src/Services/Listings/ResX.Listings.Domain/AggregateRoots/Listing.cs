using ResX.Common.Domain;
using ResX.Common.Exceptions;
using ResX.Listings.Domain.Entities;
using ResX.Listings.Domain.Enums;
using ResX.Listings.Domain.Events;
using ResX.Listings.Domain.ValueObjects;

namespace ResX.Listings.Domain.AggregateRoots;

public class Listing : AggregateRoot<Guid>
{
    private readonly List<ListingPhoto> _photos = [];

    private readonly List<string> _tags = [];

    private Listing()
    {
    }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public Guid CategoryId { get; private set; }

    public ItemCondition Condition { get; private set; }

    public TransferType TransferType { get; private set; }

    public TransferMethod TransferMethod { get; private set; }

    public ListingStatus Status { get; private set; }

    public Location Location { get; private set; } = null!;

    public Guid DonorId { get; private set; }

    public int ViewCount { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<ListingPhoto> Photos => _photos.AsReadOnly();

    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    public static Listing Create(
        string title,
        string description,
        Guid categoryId,
        ItemCondition condition,
        TransferType transferType,
        TransferMethod transferMethod,
        Location location,
        Guid donorId,
        IEnumerable<string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Title cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Description cannot be empty.");
        }

        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Category ID cannot be empty.");
        }

        if (donorId == Guid.Empty)
        {
            throw new DomainException("Donor ID cannot be empty.");
        }

        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            CategoryId = categoryId,
            Condition = condition,
            TransferType = transferType,
            TransferMethod = transferMethod,
            Location = location,
            DonorId = donorId,
            Status = ListingStatus.Draft,
            ViewCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        if (tags != null)
        {
            listing._tags.AddRange(tags.Where(t => !string.IsNullOrWhiteSpace(t)));
        }

        listing.RaiseDomainEvent(new ListingCreatedDomainEvent(listing.Id, listing.DonorId, listing.CategoryId));

        return listing;
    }

    public void Update(
        string title,
        string description,
        Guid categoryId,
        ItemCondition condition,
        TransferType transferType,
        TransferMethod transferMethod,
        Location location,
        IEnumerable<string>? tags = null)
    {
        if (Status is ListingStatus.Completed or ListingStatus.Cancelled)
        {
            throw new DomainException("Cannot update a completed or cancelled listing.");
        }

        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Category ID cannot be empty.");
        }

        Title = title;
        Description = description;
        CategoryId = categoryId;
        Condition = condition;
        TransferType = transferType;
        TransferMethod = transferMethod;
        Location = location;
        UpdatedAt = DateTime.UtcNow;

        _tags.Clear();
        if (tags != null)
        {
            _tags.AddRange(tags.Where(t => !string.IsNullOrWhiteSpace(t)));
        }
    }

    public void ChangeStatus(ListingStatus newStatus)
    {
        var previousStatus = Status;

        if (!IsValidTransition(Status, newStatus))
        {
            throw new DomainException($"Invalid status transition from {Status} to {newStatus}.");
        }

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ListingStatusChangedDomainEvent(Id, previousStatus, newStatus));
    }

    public ListingPhoto AddPhoto(string url, int displayOrder)
    {
        if (_photos.Count >= 10)
        {
            throw new DomainException("Cannot have more than 10 photos per listing.");
        }

        var photo = ListingPhoto.Create(Id, url, displayOrder);
        _photos.Add(photo);
        UpdatedAt = DateTime.UtcNow;

        return photo;
    }

    public void RemovePhoto(Guid photoId)
    {
        var photo = _photos.FirstOrDefault(p => p.Id == photoId)
                    ?? throw new NotFoundException(nameof(ListingPhoto), photoId);

        _photos.Remove(photo);
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementViewCount()
    {
        ViewCount++;
        RaiseDomainEvent(new ListingViewedDomainEvent(Id, DonorId));
    }

    public void Cancel()
    {
        if (Status is ListingStatus.Completed)
        {
            throw new DomainException("Cannot cancel a completed listing.");
        }

        ChangeStatus(ListingStatus.Cancelled);
    }

    private static bool IsValidTransition(ListingStatus current, ListingStatus target)
    {
        return (current, target) switch
        {
            (ListingStatus.Draft, ListingStatus.Active) => true,
            (ListingStatus.Draft, ListingStatus.Cancelled) => true,
            (ListingStatus.Active, ListingStatus.Reserved) => true,
            (ListingStatus.Active, ListingStatus.Moderated) => true,
            (ListingStatus.Active, ListingStatus.Cancelled) => true,
            (ListingStatus.Reserved, ListingStatus.Completed) => true,
            (ListingStatus.Reserved, ListingStatus.Active) => true,
            (ListingStatus.Reserved, ListingStatus.Cancelled) => true,
            (ListingStatus.Moderated, ListingStatus.Active) => true,
            (ListingStatus.Moderated, ListingStatus.Cancelled) => true,
            _ => false
        };
    }
}
