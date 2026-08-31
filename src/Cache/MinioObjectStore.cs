using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Microsoft.Extensions.Options;

namespace Instacord.Cache;

public sealed class MinioObjectStore : IObjectStore
{
    private readonly IMinioClient _client;
    private readonly string _bucket;

    public MinioObjectStore(IMinioClient client, IOptions<CacheOptions> options)
    {
        _client = client;
        _bucket = options.Value.Bucket;
    }

    public async Task<Stream?> GetAsync(string key, CancellationToken ct = default)
    {
        var ms = new MemoryStream();
        try
        {
            await _client.GetObjectAsync(new GetObjectArgs()
                .WithBucket(_bucket)
                .WithObject(key)
                .WithCallbackStream(async (s, _) => await s.CopyToAsync(ms, ct)), ct);
            
            ms.Position = 0;
            return ms;
        }
        catch (ObjectNotFoundException)
        {
            await ms.DisposeAsync();
            return null;
        }
    }

    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var copy = new MemoryStream();
        await content.CopyToAsync(copy, ct);
        copy.Position = 0;
        
        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(key)
            .WithStreamData(copy)
            .WithContentType(contentType)
            .WithObjectSize(copy.Length), ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _client.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(_bucket)
                .WithObject(key), ct);
        }
        catch (ObjectNotFoundException)
        {
        }
    }

    public async Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken ct = default)
    {
        var keys = new List<string>();
        var args = new ListObjectsArgs()
            .WithBucket(_bucket)
            .WithPrefix(prefix)
            .WithRecursive(true);
        await foreach (var item in _client.ListObjectsEnumAsync(args, ct))
            keys.Add(item.Key);
        return keys;
    }
}