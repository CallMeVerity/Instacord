namespace Instacord.Cache;

public interface IObjectStore
{
    Task<Stream?> GetAsync(string key, CancellationToken ct = default);
    Task PutAsync(string key, Stream content, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken ct = default);
}