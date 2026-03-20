using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResX.Files.Domain.AggregateRoots;

namespace ResX.Files.Infrastructure.Persistence.Configurations;

public class FileRecordConfiguration : IEntityTypeConfiguration<FileRecord>
{
    public void Configure(EntityTypeBuilder<FileRecord> builder)
    {
        builder.ToTable("file_records");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.OriginalName).HasColumnName("original_name").HasMaxLength(500);
        builder.Property(f => f.StorageKey).HasColumnName("storage_key").HasMaxLength(1000);
        builder.Property(f => f.Url).HasColumnName("url").HasMaxLength(2000);
        builder.Property(f => f.ContentType).HasColumnName("content_type").HasMaxLength(200);
        builder.Property(f => f.SizeBytes).HasColumnName("size_bytes");
        builder.Property(f => f.UploadedBy).HasColumnName("uploaded_by");
        builder.Property(f => f.CreatedAt).HasColumnName("created_at");
        builder.Property(f => f.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Ignore(f => f.DomainEvents);
    }
}