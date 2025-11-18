using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;
using System.Net.Http.Headers;

namespace UnitTest;

/// <summary>
/// Bridges Playwright browser with ASP.NET Core in-memory TestHost.
/// Intercepts browser HTTP requests and forwards them to WebApplicationFactory's TestServer.
/// This allows real browser testing without requiring Kestrel or external ports.
/// </summary>
public class PlaywrightTestHostProvider<TStartup> : IAsyncDisposable where TStartup : class
{
    private readonly WebApplicationFactory<TStartup> _factory;
    private readonly HttpClient _testHostClient;
    private readonly string _baseUrl;

    public PlaywrightTestHostProvider(WebApplicationFactory<TStartup>? factory = null, string baseUrl = "http://localhost")
    {
        _factory = factory ?? new WebApplicationFactory<TStartup>();
        _testHostClient = _factory.CreateClient(); // In-memory TestHost client!
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Sets up route interception on the Playwright page to forward requests to TestHost.
    /// Only intercepts requests to the base URL (localhost by default), letting external resources through.
    /// </summary>
    public async Task SetupRouteInterceptionAsync(IPage page)
    {
        await page.RouteAsync("**/*", async route =>
        {
            var request = route.Request;
            var url = request.Url;

            // Only intercept our app URLs - let CDN, external APIs, etc. go through real network
            if (!url.StartsWith(_baseUrl))
            {
                await route.ContinueAsync(); // Real network request
                return;
            }

            try
            {
                // Forward to in-memory TestHost
                var response = await ForwardToTestHostAsync(request);

                // Return TestHost response to browser
                await route.FulfillAsync(new()
                {
                    Status = (int)response.StatusCode,
                    Headers = ConvertHeaders(response),
                    ContentType = response.Content.Headers.ContentType?.ToString(),
                    BodyBytes = await response.Content.ReadAsByteArrayAsync()
                });
            }
            catch (Exception ex)
            {
                // If TestHost fails, return error to browser
                await route.FulfillAsync(new()
                {
                    Status = 500,
                    Body = $"TestHost Error: {ex.Message}"
                });
            }
        });
    }

    private async Task<HttpResponseMessage> ForwardToTestHostAsync(IRequest browserRequest)
    {
        // Create HttpRequestMessage from browser request
        var request = new HttpRequestMessage
        {
            Method = new HttpMethod(browserRequest.Method),
            RequestUri = new Uri(browserRequest.Url)
        };

        // Copy headers from browser to TestHost request
        foreach (var header in browserRequest.Headers)
        {
            // Skip headers that HttpClient sets automatically
            if (IsRestrictedHeader(header.Key))
                continue;

            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Handle POST/PUT body
        if (browserRequest.PostData != null)
        {
            request.Content = new StringContent(browserRequest.PostData);

            // Set Content-Type if provided
            if (browserRequest.Headers.TryGetValue("content-type", out var contentType))
            {
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            }
        }
        else if (browserRequest.PostDataBuffer != null)
        {
            request.Content = new ByteArrayContent(browserRequest.PostDataBuffer);

            if (browserRequest.Headers.TryGetValue("content-type", out var contentType))
            {
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            }
        }

        // Send to in-memory TestHost! 🎉
        var response = await _testHostClient.SendAsync(request);

        return response;
    }

    private static bool IsRestrictedHeader(string headerName)
    {
        // These headers are automatically set by HttpClient and shouldn't be manually added
        var restricted = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Host", "Content-Length", "Transfer-Encoding", "Connection"
        };

        return restricted.Contains(headerName);
    }

    private static Dictionary<string, string> ConvertHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>();

        // Response headers
        foreach (var header in response.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        // Content headers
        if (response.Content?.Headers != null)
        {
            foreach (var header in response.Content.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
        }

        return headers;
    }

    public async ValueTask DisposeAsync()
    {
        _testHostClient?.Dispose();
        await _factory.DisposeAsync();
    }
}
