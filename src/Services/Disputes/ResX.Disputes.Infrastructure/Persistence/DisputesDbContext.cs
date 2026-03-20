using Microsoft.EntityFrameworkCore;
using ResX.Disputes.Domain.AggregateRoots;
using ResX.Disputes.Domain.Entities;

namespace ResX.Disputes.Infrastructure.Persistence;

public class DisputesDbContext : DbContext
{
    public DisputesDbContext(DbContextOptions<DisputesDbContext> options) : base(options)
    {
    }

    public DbSet<Dispute> Disputes => Set<Dispute>();

    public DbSet<Evidence> Evidence => Set<Evidence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("disputes");

        modelBuilder.Entity<Dispute>(b =>
        {
            b.ToTable("disputes");
            b.HasKey(d => d.Id);
            b.Property(d => d.Id).HasColumnName("id");
            b.Property(d => d.TransactionId).HasColumnName("transaction_id");
            b.Property(d => d.InitiatorId).HasColumnName("initiator_id");
            b.Property(d => d.RespondentId).HasColumnName("respondent_id");
            b.Property(d => d.Reason).HasColumnName("reason").HasMaxLength(2000);
            b.Property(d => d.Status).HasColumnName("status").HasConversion<string>();
            b.Property(d => d.Resolution).HasColumnName("resolution").HasMaxLength(5000);
            b.Property(d => d.CreatedAt).HasColumnName("created_at");
            b.Property(d => d.ResolvedAt).HasColumnName("resolved_at");

            b.HasMany(d => d.Evidences)
                .WithOne()
                .HasForeignKey(e => e.DisputeId).OnDelete(DeleteBehavior.Cascade);

            b.Navigation(d => d.Evidences).HasField("_evidences").UsePropertyAccessMode(PropertyAccessMode.Field);
            b.Ignore(d => d.DomainEvents);
        });

        modelBuilder.Entity<Evidence>(b =>
        {
            b.ToTable("evidence");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.DisputeId).HasColumnName("dispute_id");
            b.Property(e => e.SubmittedBy).HasColumnName("submitted_by");
            b.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
            b.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
            b.Property(e => e.FileUrls)
                .HasColumnName("file_urls")
                .HasConversion(v =>
                    string.Join(',', v), v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
        });
    }
}