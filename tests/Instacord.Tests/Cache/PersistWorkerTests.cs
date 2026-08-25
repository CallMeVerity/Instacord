using Instacord.Cache;
using Instacord.Models;
using Instacord.Services;
using Microsoft.Extensions.Options;
using NSubstitute;
namespace Instacord.Tests.Cache;

public class PersistWorkerTests
{
    private static CacheOptions Opts(int attempts = 3, int concurrency = 1) => new()
    {
        Endpoint = "https://x", Bucket = "b", PublicBaseUrl = "https://x/b",
        AccessKey = "a", SecretKey = "s",
        PersistConcurrency = concurrency, PersistMaxAttempts = attempts
    };

    private static InstagramPost FreshPost(string code) =>
        new() { Code = code, Username = "u", Items = Array.Empty<MediaItem>() };

    private static PostPersistJob JobSub()
    {
        var store = Substitute.For<IObjectStore>();
        var fetcher = Substitute.For<IPostFetcher>();
        return Substitute.For<PostPersistJob>(store, new HttpClient(), fetcher, Options.Create(Opts()));
    }

    [Fact]
    public async Task Enqueue_runs_job_once_on_success()
    {
        var job = JobSub();
        job.RunAsync(Arg.Any<PersistRequest>(), Arg.Any<CancellationToken>()).Returns(true);
        var worker = new PersistWorker(job, Options.Create(Opts()), (_, _) => Task.CompletedTask);
        await worker.StartAsync(default);

        worker.Enqueue(new PersistRequest("ABC", FreshPost("ABC"), IsRefresh: false, OnEvict: null));
        await WaitUntilAsync(() => job.ReceivedCalls().Any());

        await worker.StopAsync(default);
        await job.Received(1).RunAsync(Arg.Any<PersistRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Retries_up_to_max_attempts_then_drops()
    {
        var job = JobSub();
        job.RunAsync(Arg.Any<PersistRequest>(), Arg.Any<CancellationToken>()).Returns(false);
        var worker = new PersistWorker(job, Options.Create(Opts(attempts: 2)), (_, _) => Task.CompletedTask);
        await worker.StartAsync(default);

        worker.Enqueue(new PersistRequest("ABC", FreshPost("ABC"), IsRefresh: false, OnEvict: null));
        await WaitUntilAsync(() => job.ReceivedCalls().Count() >= 2);

        await worker.StopAsync(default);
        await job.Received(2).RunAsync(Arg.Any<PersistRequest>(), Arg.Any<CancellationToken>());
    }

    private static async Task WaitUntilAsync(Func<bool> done, int timeoutMs = 2000)
    {
        var deadline = Environment.TickCount + timeoutMs;
        while (Environment.TickCount < deadline && !done())
            await Task.Delay(10);
        Assert.True(done(), "condition never met within timeout");
    }
}