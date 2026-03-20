namespace ResX.Storage.S3;

public class S3Options
{
    public const string SectionName = "YandexS3";

    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = "https://storage.yandexcloud.net";
    public string Region { get; set; } = "ru-central1";
}
