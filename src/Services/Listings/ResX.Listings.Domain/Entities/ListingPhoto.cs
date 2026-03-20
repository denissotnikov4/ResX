using ResX.Common.Domain;

namespace ResX.Listings.Domain.Entities;

public class ListingPhoto : Entity<Guid>
{
    private ListingPhoto()
    {
    }

    public string Url { get; private set; } = string.Empty;

    public int DisplayOrder { get; private set; }

    public Guid ListingId { get; private set; }

    public static ListingPhoto Create(Guid listingId, string url, int displayOrder)
    {
        return new ListingPhoto
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            Url = url,
            DisplayOrder = displayOrder
        };
    }
}