namespace Instacord.Cache;

public static class CacheKeys
{
    public const string Prefix = "posts/";

    public static string MetaKey(string code) => $"posts/{code}/meta.json";

    public static string MediaKey(string code, int index, string ext) => $"posts/{code}/{index}.{ext}";

    public static string CodePrefix(string code) => $"posts/{code}/";

    public static string PublicMediaUrl(string publicBaseUrl, string code, int index, string ext) =>
        $"{publicBaseUrl.TrimEnd('/')}/posts/{code}/{index}.{ext}";
}