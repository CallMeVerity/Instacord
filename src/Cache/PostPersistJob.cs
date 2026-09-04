using System.Text.Json;
using Instacord.Models;
using Instacord.Services;
using Microsoft.Extensions.Options;

namespace Instacord.Cache;

public class PostPersistJob
{
    private readonly IObjectStore _store;
    private readonly HttpClient _http;
    private readonly IPostFetcher _fetcher;
    private readonly CacheOptions _options;

    public PostPersistJob(IObjectStore store, HttpClient http, IPostFetcher fetcher, IOptions<CacheOptions> options)
    {
        _store = store;
        _http = http;
        _fetcher = fetcher;
        _options = options.Value;
    }

    public virtual async Task<bool> RunAsync(PersistRequest request, CancellationToken ct)
    {
        var post = request.FreshPost;

        if (!request.IsRefresh) 
            return await PersistAsync(request.Code, post!, ct);
        
        post = await _fetcher.FetchPostAsync(request.Code, ct);
        
        if (post is not null)
            return await PersistAsync(request.Code, post!, ct);
        
        await EvictAsync(request.Code, ct);
        request.OnEvict?.Invoke(request.Code);
        
        return true;
    }

    private async Task<bool> PersistAsync(string code, InstagramPost post, CancellationToken ct)
    {
        var storedItems = new List<MediaItem>();
        for (var i = 0; i < post.Items.Count; i++)
        {
            var item = post.Items[i];
            var index = i + 1;
            try
            {
                await using var media = await _http.GetStreamAsync(item.MediaUrl, ct);
                using var buffer = new MemoryStream();
                await media.CopyToAsync(buffer, ct);
                buffer.Position = 0;
                var (ext, contentType) = MimeFor(item.Type);
                await _store.PutAsync(CacheKeys.MediaKey(code, index, ext), buffer, contentType, ct);
                storedItems.Add(item with { MediaUrl = CacheKeys.PublicMediaUrl(_options.PublicBaseUrl, code, index, ext), IsCached = true });
            }
            catch
            {
                return false;
            }
        }

        var stored = post with { Items = storedItems };
        var envelope = new CachedPostEnvelope { CachedAt = DateTimeOffset.UtcNow, Post = stored };
        using var meta = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(envelope));
        await _store.PutAsync(CacheKeys.MetaKey(code), meta, "application/json", ct);
        return true;
    }

    private async Task EvictAsync(string code, CancellationToken ct)
    {
        var keys = await _store.ListAsync(CacheKeys.CodePrefix(code), ct);
        foreach (var key in keys)
            await _store.DeleteAsync(key, ct);
    }

    private static (string ext, string contentType) MimeFor(MediaType type) =>
        type == MediaType.Video ? ("mp4", "video/mp4") : ("jpg", "image/jpeg");
}