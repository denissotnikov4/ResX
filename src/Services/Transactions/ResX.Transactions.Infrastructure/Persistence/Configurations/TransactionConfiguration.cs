using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResX.Transactions.Domain.AggregateRoots;

namespace ResX.Transactions.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.ListingId).HasColumnName("listing_id");
        builder.Property(t => t.DonorId).HasColumnName("donor_id");
        builder.Property(t => t.RecipientId).HasColumnName("recipient_id");
        builder.Property(t => t.Type).HasColumnName("type").HasConversion<string>();
        builder.Property(t => t.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(t => t.Notes).HasColumnName("notes").HasMaxLength(1000);
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.CompletedAt).HasColumnName("completed_at");
        builder.Ignore(t => t.DomainEvents);

        builder.HasIndex(t => t.DonorId).HasDatabaseName("ix_transactions_donor_id");
        builder.HasIndex(t => t.RecipientId).HasDatabaseName("ix_transactions_recipient_id");
        builder.HasIndex(t => t.ListingId).HasDatabaseName("ix_transactions_listing_id");
    }
}