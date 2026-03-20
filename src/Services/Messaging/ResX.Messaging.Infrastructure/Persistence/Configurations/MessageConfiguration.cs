using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResX.Messaging.Domain.Entities;

namespace ResX.Messaging.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.ConversationId).HasColumnName("conversation_id");
        builder.Property(m => m.SenderId).HasColumnName("sender_id");
        builder.Property(m => m.Content).HasColumnName("content").HasMaxLength(2000);
        builder.Property(m => m.SentAt).HasColumnName("sent_at");
        builder.Property(m => m.IsRead).HasColumnName("is_read").HasDefaultValue(false);
    }
}