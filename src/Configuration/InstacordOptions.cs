namespace Instacord.Configuration;

public class InstacordOptions
{
    public required string UserAgent { get; set; }
    public int RequestTimeoutSeconds { get; set; } = 15;
    public string CookieHeader { get; set; } = "";
}