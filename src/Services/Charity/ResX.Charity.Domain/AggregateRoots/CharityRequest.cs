using ResX.Charity.Domain.Entities;
using ResX.Charity.Domain.Enums;
using ResX.Common.Domain;
using ResX.Common.Exceptions;

namespace ResX.Charity.Domain.AggregateRoots;

public class CharityRequest : AggregateRoot<Guid>
{
    private readonly List<RequestedItem> _requestedItems = [];

    private CharityRequest()
    {
    }

    public Guid OrganizationId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public CharityRequestStatus Status { get; private set; }

    public DateTime? DeadlineDate { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<RequestedItem> RequestedItems => _requestedItems.AsReadOnly();

    public static CharityRequest Create(
        Guid organizationId,
        string title,
        string description,
        DateTime? deadlineDate = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Title is required.");
        }

        if (organizationId == Guid.Empty)
        {
            throw new DomainException("Organization ID is required.");
        }

        return new CharityRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Title = title,
            Description = description,
            Status = CharityRequestStatus.Active,
            DeadlineDate = deadlineDate,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddRequestedItem(Guid categoryId, string categoryName, int quantityNeeded, string condition)
    {
        var item = RequestedItem.Create(Id, categoryId, categoryName, quantityNeeded, condition);
        _requestedItems.Add(item);
    }

    public void Complete()
    {
        Status = CharityRequestStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = CharityRequestStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}