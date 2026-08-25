using Instacord.Models;

namespace Instacord.Services;

public interface IPostFetcher
{
    Task<string?> ResolveCodeAsync(string code, bool isShare, CancellationToken ct = default);
    Task<InstagramPost?> FetchPostAsync(string code, CancellationToken ct = default);
}