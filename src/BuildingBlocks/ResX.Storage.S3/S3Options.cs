namespace ResX.Storage.S3;

public class S3Options
{
    public const string SectionName = "S3";

    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = "http://minio:9000";
    public string? PublicUrl { get; set; }
    public string Region { get; set; } = "us-east-1";
}
