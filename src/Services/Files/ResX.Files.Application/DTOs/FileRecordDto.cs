namespace ResX.Files.Application.DTOs;

public record FileRecordDto(
    Guid Id,
    string OriginalName,
    string StorageKey,
    string Url,
    string ContentType,
    long SizeBytes,
    Guid UploadedBy,
    DateTime CreatedAt);
