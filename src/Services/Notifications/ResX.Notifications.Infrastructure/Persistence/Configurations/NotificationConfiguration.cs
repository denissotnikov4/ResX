using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResX.Notifications.Domain.AggregateRoots;

namespace ResX.Notifications.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id");
        builder.Property(n => n.UserId).HasColumnName("user_id");
        builder.Property(n => n.Type).HasColumnName("type").HasConversion<string>();
        builder.Property(n => n.Title).HasColumnName("title").HasMaxLength(200);
        builder.Property(n => n.Body).HasColumnName("body").HasMaxLength(1000);
        builder.Property(n => n.IsRead).HasColumnName("is_read").HasDefaultValue(false);
        builder.Property(n => n.CreatedAt).HasColumnName("created_at");
        builder.Property(n => n.ReadAt).HasColumnName("read_at");

        builder.Property(n => n.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb");

        builder.Ignore(n => n.DomainEvents);

        builder.HasIndex(n => n.UserId).HasDatabaseName("ix_notifications_user_id");
        builder.HasIndex(n => n.IsRead).HasDatabaseName("ix_notifications_is_read");
    }
}