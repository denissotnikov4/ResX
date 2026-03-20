using Microsoft.EntityFrameworkCore;
using ResX.Charity.Domain.AggregateRoots;
using ResX.Charity.Domain.Entities;

namespace ResX.Charity.Infrastructure.Persistence;

public class CharityDbContext : DbContext
{
    public CharityDbContext(DbContextOptions<CharityDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<CharityRequest> CharityRequests => Set<CharityRequest>();

    public DbSet<RequestedItem> RequestedItems => Set<RequestedItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("charity");

        modelBuilder.Entity<Organization>(b =>
        {
            b.ToTable("organizations");
            b.HasKey(o => o.Id);
            b.Property(o => o.Id).HasColumnName("id");
            b.Property(o => o.UserId).HasColumnName("user_id");
            b.Property(o => o.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(o => o.Description).HasColumnName("description").HasMaxLength(2000);
            b.Property(o => o.VerificationStatus).HasColumnName("verification_status").HasConversion<string>();
            b.Property(o => o.LegalDocumentUrl).HasColumnName("legal_document_url").HasMaxLength(1000);
            b.Property(o => o.CreatedAt).HasColumnName("created_at");
            b.Ignore(o => o.DomainEvents);
        });

        modelBuilder.Entity<CharityRequest>(b =>
        {
            b.ToTable("charity_requests");
            b.HasKey(r => r.Id);
            b.Property(r => r.Id).HasColumnName("id");
            b.Property(r => r.OrganizationId).HasColumnName("organization_id");
            b.Property(r => r.Title).HasColumnName("title").HasMaxLength(200);
            b.Property(r => r.Description).HasColumnName("description").HasMaxLength(5000);
            b.Property(r => r.Status).HasColumnName("status").HasConversion<string>();
            b.Property(r => r.DeadlineDate).HasColumnName("deadline_date");
            b.Property(r => r.CreatedAt).HasColumnName("created_at");
            b.Property(r => r.UpdatedAt).HasColumnName("updated_at");
            b.HasMany(r => r.RequestedItems)
                .WithOne()
                .HasForeignKey(i => i.CharityRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            b.Ignore(r => r.DomainEvents);
        });

        modelBuilder.Entity<RequestedItem>(b =>
        {
            b.ToTable("requested_items");
            b.HasKey(i => i.Id);
            b.Property(i => i.Id).HasColumnName("id");
            b.Property(i => i.CharityRequestId).HasColumnName("charity_request_id");
            b.Property(i => i.CategoryId).HasColumnName("category_id");
            b.Property(i => i.CategoryName).HasColumnName("category_name").HasMaxLength(100);
            b.Property(i => i.QuantityNeeded).HasColumnName("quantity_needed");
            b.Property(i => i.QuantityReceived).HasColumnName("quantity_received");
            b.Property(i => i.Condition).HasColumnName("condition").HasMaxLength(50);
        });
    }
}