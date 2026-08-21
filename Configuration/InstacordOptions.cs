namespace Instacord.Configuration;

public class InstacordOptions
{
    public required string UserAgent { get; set; }
    public int RequestTimeoutSeconds { get; set; } = 15;
    public int CacheSeconds { get; set; } = 900;
    public string CookieHeader { get; set; } = "";
}