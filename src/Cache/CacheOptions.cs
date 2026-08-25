namespace Instacord.Cache;

public class CacheOptions
{
    public string Endpoint { get; set; } = "";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string Bucket { get; set; } = "instacord";
    public string PublicBaseUrl { get; set; } = "";
    public int RefreshAgeHours { get; set; } = 24;
    public int TtlDays { get; set; } = 7;
    public int MemoryMaxPosts { get; set; } = 500;
    public int PersistConcurrency { get; set; } = 3;
    public int PersistMaxAttempts { get; set; } = 3;
}