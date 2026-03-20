using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResX.Users.Domain.Aggregates;

namespace ResX.Users.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        builder.Property(p => p.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(1000);
        builder.Property(p => p.Bio).HasColumnName("bio").HasMaxLength(1000);
        builder.Property(p => p.City).HasColumnName("city").HasMaxLength(100);
        builder.Property(p => p.Rating).HasColumnName("rating").HasColumnType("decimal(3,2)").HasDefaultValue(0m);
        builder.Property(p => p.ReviewCount).HasColumnName("review_count").HasDefaultValue(0);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        builder.OwnsOne(p => p.EcoStats, eco =>
        {
            eco.Property(e => e.ItemsGifted).HasColumnName("items_gifted").HasDefaultValue(0);
            eco.Property(e => e.ItemsReceived).HasColumnName("items_received").HasDefaultValue(0);
            eco.Property(e => e.Co2SavedKg).HasColumnName("co2_saved_kg").HasColumnType("decimal(10,2)").HasDefaultValue(0m);
            eco.Property(e => e.WasteSavedKg).HasColumnName("waste_saved_kg").HasColumnType("decimal(10,2)").HasDefaultValue(0m);
        });

        builder.HasMany(p => p.Reviews)
            .WithOne()
            .HasForeignKey(r => r.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Reviews).HasField("_reviews").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(p => p.DomainEvents);
    }
}