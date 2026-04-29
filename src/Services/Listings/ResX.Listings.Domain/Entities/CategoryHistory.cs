using ResX.Common.Domain;

namespace ResX.Listings.Domain.Entities;

public class CategoryHistory : Entity<Guid>
{
    private CategoryHistory()
    {
    }

    public Guid CategoryId { get; private set; }

    public Guid ChangedByUserId { get; private set; }

    public CategoryChangeType ChangeType { get; private set; }

    public string? OldValuesJson { get; private set; }

    public string? NewValuesJson { get; private set; }

    public DateTime ChangedAt { get; private set; }

    public static CategoryHistory Create(
        Guid categoryId,
        Guid changedByUserId,
        CategoryChangeType changeType,
        string? oldValuesJson,
        string? newValuesJson)
    {
        return new CategoryHistory
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            ChangedByUserId = changedByUserId,
            ChangeType = changeType,
            OldValuesJson = oldValuesJson,
            NewValuesJson = newValuesJson,
            ChangedAt = DateTime.UtcNow
        };
    }
}

public enum CategoryChangeType
{
    Created = 1,
    Updated = 2,
    Deactivated = 3,
    Reactivated = 4
}
