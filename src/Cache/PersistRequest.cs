using Instacord.Models;

namespace Instacord.Cache;

public sealed record PersistRequest(
    string Code,
    InstagramPost? FreshPost,
    bool IsRefresh,
    Action<string>? OnEvict);