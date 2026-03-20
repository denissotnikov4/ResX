namespace ResX.Caching.Redis;

public class CacheOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";
    public int DefaultExpiryMinutes { get; set; } = 60;
}
