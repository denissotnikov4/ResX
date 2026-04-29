using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResX.Listings.Domain.Entities;

namespace ResX.Listings.Infrastructure.Persistence.Configurations;

public class CategoryHistoryConfiguration : IEntityTypeConfiguration<CategoryHistory>
{
    public void Configure(EntityTypeBuilder<CategoryHistory> builder)
    {
        builder.ToTable("category_history");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(h => h.ChangedByUserId).HasColumnName("changed_by_user_id").IsRequired();
        builder.Property(h => h.ChangeType).HasColumnName("change_type").HasConversion<string>().IsRequired();
        builder.Property(h => h.OldValuesJson).HasColumnName("old_values_json").HasColumnType("jsonb");
        builder.Property(h => h.NewValuesJson).HasColumnName("new_values_json").HasColumnType("jsonb");
        builder.Property(h => h.ChangedAt).HasColumnName("changed_at").IsRequired();

        builder.HasIndex(h => h.CategoryId).HasDatabaseName("ix_category_history_category_id");
    }
}
