using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResX.Listings.Domain.AggregateRoots;

namespace ResX.Listings.Infrastructure.Persistence.Configurations;

public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable("listings");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(l => l.Description).HasColumnName("description").HasMaxLength(5000).IsRequired();
        builder.Property(l => l.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(l => l.Condition).HasColumnName("condition").HasConversion<string>().IsRequired();
        builder.Property(l => l.TransferType).HasColumnName("transfer_type").HasConversion<string>().IsRequired();
        builder.Property(l => l.TransferMethod).HasColumnName("transfer_method").HasConversion<string>().IsRequired();
        builder.Property(l => l.Status).HasColumnName("status").HasConversion<string>().IsRequired();
        builder.Property(l => l.DonorId).HasColumnName("donor_id").IsRequired();
        builder.Property(l => l.ViewCount).HasColumnName("view_count").HasDefaultValue(0);
        builder.Property(l => l.WeightGrams).HasColumnName("weight_grams").IsRequired();
        builder.Property(l => l.Co2SavedG).HasColumnName("co2_saved_g").IsRequired();
        builder.Property(l => l.WasteSavedG).HasColumnName("waste_saved_g").IsRequired();
        builder.Property(l => l.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(l => l.UpdatedAt).HasColumnName("updated_at");

        builder.OwnsOne(l => l.Location, location =>
        {
            location.Property(loc => loc.City).HasColumnName("city").HasMaxLength(100);
            location.Property(loc => loc.District).HasColumnName("district").HasMaxLength(100);
            location.Property(loc => loc.Latitude).HasColumnName("latitude");
            location.Property(loc => loc.Longitude).HasColumnName("longitude");
        });

        builder.Property(l => l.Tags)
            .HasColumnName("tags")
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasMaxLength(1000);

        builder.HasMany(l => l.Photos)
            .WithOne()
            .HasForeignKey(p => p.ListingId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(l => l.Photos).HasField("_photos").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(l => l.DomainEvents);

        builder.HasIndex(l => l.DonorId).HasDatabaseName("ix_listings_donor_id");
        builder.HasIndex(l => l.Status).HasDatabaseName("ix_listings_status");
        builder.HasIndex(l => l.CreatedAt).HasDatabaseName("ix_listings_created_at");
        builder.HasIndex(l => l.CategoryId).HasDatabaseName("ix_listings_category_id");
    }
}
