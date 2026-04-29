using ResX.Common.Domain;
using ResX.Common.Exceptions;

namespace ResX.Listings.Domain.AggregateRoots;

public class Category : AggregateRoot<Guid>
{
    private Category()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Guid? ParentCategoryId { get; private set; }

    public string? IconUrl { get; private set; }

    public bool IsActive { get; private set; }

    public int DisplayOrder { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public static Category Create(
        string name,
        string? description,
        Guid? parentCategoryId,
        string? iconUrl,
        int displayOrder)
    {
        Validate(name, displayOrder);

        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ParentCategoryId = parentCategoryId,
            IconUrl = string.IsNullOrWhiteSpace(iconUrl) ? null : iconUrl.Trim(),
            IsActive = true,
            DisplayOrder = displayOrder,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string name,
        string? description,
        Guid? parentCategoryId,
        string? iconUrl,
        int displayOrder)
    {
        Validate(name, displayOrder);

        if (parentCategoryId.HasValue && parentCategoryId.Value == Id)
            throw new DomainException("Category cannot be its own parent.");

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ParentCategoryId = parentCategoryId;
        IconUrl = string.IsNullOrWhiteSpace(iconUrl) ? null : iconUrl.Trim();
        DisplayOrder = displayOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("Category is already inactive.");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        if (IsActive)
            throw new DomainException("Category is already active.");

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void Validate(string name, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name cannot be empty.");

        if (name.Length > 100)
            throw new DomainException("Category name cannot exceed 100 characters.");

        if (displayOrder < 0)
            throw new DomainException("Display order cannot be negative.");
    }
}
