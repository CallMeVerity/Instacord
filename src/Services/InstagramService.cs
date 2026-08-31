using System.Net;
using Instacord.Configuration;
using Instacord.Models;
using Instacord.Parsing;
using Microsoft.Extensions.Options;

namespace Instacord.Services;

public class InstagramService : IPostFetcher
{
    private const string BaseUrl = "https://www.instagram.com/";

    private readonly HttpClient _http;
    private readonly InstacordOptions _options;
    private readonly CookieContainer _cookies;
    private int _warmedUp;

    public InstagramService(HttpClient http, IOptions<InstacordOptions> options, CookieContainer cookies)
    {
        _http = http;
        _options = options.Value;
        _cookies = cookies;
        _http.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(_options.UserAgent);
        if (!string.IsNullOrWhiteSpace(_options.CookieHeader))
            ApplyCookieHeader(_options.CookieHeader);
    }

    public async Task<string?> ResolveCodeAsync(string code, bool isShare, CancellationToken ct = default)
    {
        if (!isShare)
            return code;

        await WarmUpAsync(ct);
        return await ResolveShareAsync(code, ct);
    }

    public async Task<InstagramPost?> FetchPostAsync(string code, CancellationToken ct = default)
    {
        await WarmUpAsync(ct);
        var url = $"{BaseUrl}p/{code}/";
        var html = await FetchAsync(url, ct);
        if (html is null)
            return null;

        try
        {
            return PostParser.Parse(html);
        }
        catch (InstagramParseException)
        {
            return null;
        }
    }

    private async Task WarmUpAsync(CancellationToken ct)
    {
        if (Interlocked.Exchange(ref _warmedUp, 1) == 1)
            return;

        try
        {
            using var response = await _http.GetAsync(BaseUrl, ct);
        }
        catch
        {
            // ignored
        }
    }

    private async Task<string?> ResolveShareAsync(string token, CancellationToken ct)
    {
        var shareUrl = $"{BaseUrl}share/{token}/";
        try
        {
            using var response = await _http.GetAsync(shareUrl, ct);
            var finalUri = response.RequestMessage?.RequestUri?.AbsoluteUri ?? shareUrl;
            var parsed = InstagramUrlParser.TryParse(finalUri);
            return parsed is { IsShare: false } ? parsed.Code : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> FetchAsync(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyBrowserHeaders(request);
        using var response = await _http.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private static void ApplyBrowserHeaders(HttpRequestMessage request)
    {
        request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        request.Headers.Add("sec-ch-ua", "\"Chromium\";v=\"124\", \"Google Chrome\";v=\"124\", \"Not-A.Brand\";v=\"99\"");
        request.Headers.Add("sec-ch-ua-mobile", "?0");
        request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
        request.Headers.Add("Sec-Fetch-Dest", "document");
        request.Headers.Add("Sec-Fetch-Mode", "navigate");
        request.Headers.Add("Sec-Fetch-Site", "same-origin");
        request.Headers.Add("Sec-Fetch-User", "?1");
        request.Headers.Add("Upgrade-Insecure-Requests", "1");
        request.Headers.Referrer = new Uri(BaseUrl);
    }

    private void ApplyCookieHeader(string header)
    {
        foreach (var part in header.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0)
                continue;
            
            var name = part[..eq].Trim();
            var value = part[(eq + 1)..].Trim();
            
            try
            {
                _cookies.Add(new Uri(BaseUrl), new Cookie(name, value));
            }
            catch
            {
                // ignored
            }
        }
    }
}