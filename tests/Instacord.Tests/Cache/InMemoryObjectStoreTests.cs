using Instacord.Cache;
namespace Instacord.Tests.Cache;

public class InMemoryObjectStoreTests
{
    [Fact]
    public async Task Put_then_Get_roundtrips_bytes()
    {
        var store = new InMemoryObjectStore();
        var bytes = "hello"u8.ToArray();
        using var ms = new MemoryStream(bytes);
        await store.PutAsync("k", ms, "text/plain");

        var got = await store.GetAsync("k");
        Assert.NotNull(got);
        using var reader = new StreamReader(got!);
        Assert.Equal("hello", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task Get_missing_returns_null()
    {
        var store = new InMemoryObjectStore();
        Assert.Null(await store.GetAsync("missing"));
    }

    [Fact]
    public async Task Delete_removes_object()
    {
        var store = new InMemoryObjectStore();
        using var ms = new MemoryStream("x"u8.ToArray());
        await store.PutAsync("k", ms, "text/plain");
        await store.DeleteAsync("k");
        Assert.Null(await store.GetAsync("k"));
    }

    [Fact]
    public async Task List_returns_keys_under_prefix()
    {
        var store = new InMemoryObjectStore();
        await store.PutAsync("posts/A/1.jpg", new MemoryStream("a"u8.ToArray()), "image/jpeg");
        await store.PutAsync("posts/A/meta.json", new MemoryStream("b"u8.ToArray()), "application/json");
        await store.PutAsync("posts/B/meta.json", new MemoryStream("c"u8.ToArray()), "application/json");

        var keys = await store.ListAsync("posts/A/");
        Assert.Equal(2, keys.Count);
        Assert.Contains("posts/A/1.jpg", keys);
        Assert.Contains("posts/A/meta.json", keys);
    }
}