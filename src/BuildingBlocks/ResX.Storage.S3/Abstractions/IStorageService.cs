namespace ResX.Storage.S3.Abstractions;

public interface IStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileKey, CancellationToken cancellationToken = default);
    Task<string> GetPresignedUrlAsync(string fileKey, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
}
