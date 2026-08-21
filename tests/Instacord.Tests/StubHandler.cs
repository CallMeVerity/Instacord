using System.Net;
using System.Text;

namespace Instacord.Tests;

public sealed class StubHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Code, string Body, string? ContentType)> _responses = new();
    private readonly Dictionary<string, string> _redirects = new();

    public int CallCount { get; private set; }
    public IList<string> RequestedUris { get; } = new List<string>();

    public void RespondWith(string uri, HttpStatusCode code, string body, string? contentType = null) =>
        _responses[uri] = (code, body, contentType ?? "text/html");

    public void RedirectFrom(string from, string finalUrl) => _redirects[from] = finalUrl;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        var key = request.RequestUri!.AbsoluteUri;
        RequestedUris.Add(key);

        if (_redirects.TryGetValue(key, out var finalUrl))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, finalUrl),
            });
        }

        if (_responses.TryGetValue(key, out var resp))
        {
            return Task.FromResult(new HttpResponseMessage(resp.Code)
            {
                Content = new StringContent(resp.Body, Encoding.UTF8, resp.ContentType ?? "text/html"),
                RequestMessage = request,
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
    }
}