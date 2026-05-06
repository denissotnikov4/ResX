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

    /// <summary>Grams of CO2 saved per 100 grams of product transferred in this category.</summary>
    public int Co2SavedPer100GramsG { get; private set; }

    /// <summary>Grams of waste diverted from landfill per 100 grams of product transferred.</summary>
    public int WasteSavedPer100GramsG { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public static Category Create(
        string name,
        string? description,
        Guid? parentCategoryId,
        string? iconUrl,
        int displayOrder,
        int co2SavedPer100GramsG,
        int wasteSavedPer100GramsG)
    {
        Validate(name, displayOrder, co2SavedPer100GramsG, wasteSavedPer100GramsG);

        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ParentCategoryId = parentCategoryId,
            IconUrl = string.IsNullOrWhiteSpace(iconUrl) ? null : iconUrl.Trim(),
            IsActive = true,
            DisplayOrder = displayOrder,
            Co2SavedPer100GramsG = co2SavedPer100GramsG,
            WasteSavedPer100GramsG = wasteSavedPer100GramsG,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string name,
        string? description,
        Guid? parentCategoryId,
        string? iconUrl,
        int displayOrder,
        int co2SavedPer100GramsG,
        int wasteSavedPer100GramsG)
    {
        Validate(name, displayOrder, co2SavedPer100GramsG, wasteSavedPer100GramsG);

        if (parentCategoryId.HasValue && parentCategoryId.Value == Id)
            throw new DomainException("Category cannot be its own parent.");

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ParentCategoryId = parentCategoryId;
        IconUrl = string.IsNullOrWhiteSpace(iconUrl) ? null : iconUrl.Trim();
        DisplayOrder = displayOrder;
        Co2SavedPer100GramsG = co2SavedPer100GramsG;
        WasteSavedPer100GramsG = wasteSavedPer100GramsG;
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

    private static void Validate(string name, int displayOrder, int co2Per100, int wastePer100)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name cannot be empty.");

        if (name.Length > 100)
            throw new DomainException("Category name cannot exceed 100 characters.");

        if (displayOrder < 0)
            throw new DomainException("Display order cannot be negative.");

        if (co2Per100 < 0)
            throw new DomainException("CO2 rate cannot be negative.");

        if (wastePer100 < 0)
            throw new DomainException("Waste rate cannot be negative.");
    }
}
