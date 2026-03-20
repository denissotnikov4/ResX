using ResX.Common.Domain;

namespace ResX.Files.Domain.AggregateRoots;

public class FileRecord : AggregateRoot<Guid>
{
    private FileRecord()
    {
    }

    public string OriginalName { get; private set; } = string.Empty;


    public string StorageKey { get; private set; } = string.Empty;

    public string Url { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public Guid UploadedBy { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public static FileRecord Create(
        string originalName,
        string storageKey,
        string url,
        string contentType,
        long sizeBytes,
        Guid uploadedBy)
    {
        return new FileRecord
        {
            Id = Guid.NewGuid(),
            OriginalName = originalName,
            StorageKey = storageKey,
            Url = url,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            UploadedBy = uploadedBy,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void MarkDeleted()
    {
        IsDeleted = true;
    }
}