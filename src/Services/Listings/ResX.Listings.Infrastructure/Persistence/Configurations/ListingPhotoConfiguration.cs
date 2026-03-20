using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResX.Listings.Domain.Entities;

namespace ResX.Listings.Infrastructure.Persistence.Configurations;

public class ListingPhotoConfiguration : IEntityTypeConfiguration<ListingPhoto>
{
    public void Configure(EntityTypeBuilder<ListingPhoto> builder)
    {
        builder.ToTable("listing_photos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").IsRequired().ValueGeneratedNever();
        builder.Property(p => p.ListingId).HasColumnName("listing_id");
        builder.Property(p => p.Url).HasColumnName("url").HasMaxLength(1000).IsRequired();
        builder.Property(p => p.DisplayOrder).HasColumnName("display_order");
    }
}