using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResX.Storage.S3.Abstractions;

namespace ResX.Storage.S3;

public class YandexS3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<YandexS3StorageService> _logger;

    public YandexS3StorageService(
        IOptions<S3Options> options,
        ILogger<YandexS3StorageService> logger)
    {
        _logger = logger;
        var opts = options.Value;
        _bucketName = opts.BucketName;

        var config = new AmazonS3Config
        {
            ServiceURL = opts.ServiceUrl,
            AuthenticationRegion = opts.Region,
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(opts.AccessKey, opts.SecretKey, config);
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var key = $"{Guid.NewGuid()}/{fileName}";

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            AutoCloseStream = false
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);

        _logger.LogInformation("File uploaded to S3: {Key}", key);
        return key;
    }

    public async Task DeleteAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = fileKey
        };

        await _s3Client.DeleteObjectAsync(request, cancellationToken);
        _logger.LogInformation("File deleted from S3: {Key}", fileKey);
    }

    public async Task<string> GetPresignedUrlAsync(
        string fileKey,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = fileKey,
            Expires = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromHours(1)),
            Verb = HttpVerb.GET
        };

        var url = await _s3Client.GetPreSignedURLAsync(request);
        return url;
    }
}
