using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResX.Listings.Domain.AggregateRoots;

namespace ResX.Listings.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(c => c.ParentCategoryId).HasColumnName("parent_category_id");
        builder.Property(c => c.IconUrl).HasColumnName("icon_url").HasMaxLength(500);
        builder.Property(c => c.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(c => c.DisplayOrder).HasColumnName("display_order").IsRequired();
        builder.Property(c => c.Co2SavedPer100GramsG).HasColumnName("co2_saved_per_100g_g").IsRequired();
        builder.Property(c => c.WasteSavedPer100GramsG).HasColumnName("waste_saved_per_100g_g").IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        builder.Ignore(c => c.DomainEvents);

        builder.HasIndex(c => c.ParentCategoryId).HasDatabaseName("ix_categories_parent_id");
    }
}
