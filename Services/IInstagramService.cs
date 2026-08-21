using Instacord.Models;

namespace Instacord.Services;

public interface IInstagramService
{
    Task<InstagramPost?> GetPostAsync(string code, CancellationToken ct = default, bool isShare = false);
}