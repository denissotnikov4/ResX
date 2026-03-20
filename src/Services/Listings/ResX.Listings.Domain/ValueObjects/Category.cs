using ResX.Common.Domain;
using ResX.Common.Exceptions;

namespace ResX.Listings.Domain.ValueObjects;

public sealed class Category : ValueObject
{
    private Category(Guid id, string name, Guid? parentCategoryId)
    {
        Id = id;
        Name = name;
        ParentCategoryId = parentCategoryId;
    }

    public Guid Id { get; }

    public string Name { get; }

    public Guid? ParentCategoryId { get; }

    public static Category Create(Guid id, string name, Guid? parentCategoryId = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Category ID cannot be empty.");
        }

        return string.IsNullOrWhiteSpace(name) 
            ? throw new DomainException("Category name cannot be empty.") 
            : new Category(id, name, parentCategoryId);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
        yield return Name;
        yield return ParentCategoryId;
    }
}