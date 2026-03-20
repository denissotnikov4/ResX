using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResX.Users.Domain.Entities;

namespace ResX.Users.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");
        builder.HasKey(r => r.Id);
        builder.Property(p => p.Id).HasColumnName("id").IsRequired().ValueGeneratedNever();
        builder.Property(r => r.UserProfileId).HasColumnName("user_profile_id");
        builder.Property(r => r.ReviewerId).HasColumnName("reviewer_id");
        builder.Property(r => r.ReviewerName).HasColumnName("reviewer_name").HasMaxLength(200);
        builder.Property(r => r.Rating).HasColumnName("rating");
        builder.Property(r => r.Comment).HasColumnName("comment").HasMaxLength(2000);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
    }
}