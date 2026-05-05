using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResX.Storage.S3.Abstractions;

namespace ResX.Storage.S3;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly IAmazonS3 _presignClient;
    private readonly Uri? _publicUriOverride;
    private readonly string _bucketName;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(
        IOptions<S3Options> options,
        ILogger<S3StorageService> logger)
    {
        _logger = logger;
        var opts = options.Value;
        _bucketName = opts.BucketName;

        _s3Client = BuildClient(opts.ServiceUrl, opts);

        var presignUrl = string.IsNullOrWhiteSpace(opts.PublicUrl) ? opts.ServiceUrl : opts.PublicUrl;
        _presignClient = presignUrl == opts.ServiceUrl
            ? _s3Client
            : BuildClient(presignUrl, opts);

        _publicUriOverride = !string.IsNullOrWhiteSpace(opts.PublicUrl)
            ? new Uri(opts.PublicUrl)
            : null;
    }

    private static IAmazonS3 BuildClient(string serviceUrl, S3Options opts) =>
        new AmazonS3Client(opts.AccessKey, opts.SecretKey, new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            AuthenticationRegion = opts.Region,
            ForcePathStyle = true,
            UseHttp = serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        });

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

        var url = await _presignClient.GetPreSignedURLAsync(request);

        if (_publicUriOverride is not null)
        {
            var generated = new Uri(url);
            url = $"{_publicUriOverride.Scheme}://{_publicUriOverride.Authority}{generated.PathAndQuery}";
        }

        return url;
    }
}
