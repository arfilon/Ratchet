#nullable disable
using Microsoft.Playwright;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Arfilon.Ratchet;

/// <summary>
/// Ratchet browser automation for ASP.NET Core testing, powered by Playwright.
/// Provides in-memory TestHost integration for fast, isolated testing.
/// </summary>
public class Ratchet<TSetup> : IDisposable, IAsyncDisposable where TSetup : class
{
    public int DefaultTimeout = 20000;

    private readonly WebApplicationFactory<TSetup> _factory;
    private readonly HttpClient _testHostClient;
    private readonly string _baseUrl = "http://localhost";

    private IPlaywright _playwright;
    private IBrowser _browser;
    private IPage _page;
    private IBrowserContext _context;

    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public event Action<object> OnConsoleLog;

    // Constructors matching original API
    public Ratchet(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate, string baseAddress = null)
    {
        _factory = CreateFactory(builder => builder.ConfigureAppConfiguration(configureDelegate));
        _testHostClient = _factory.CreateClient();
        if (!string.IsNullOrWhiteSpace(baseAddress))
            _baseUrl = baseAddress;
    }

    public Ratchet(Action<WebHostBuilder> configureDelegate, string baseAddress = null)
    {
        _factory = CreateFactory(configureDelegate);
        _testHostClient = _factory.CreateClient();
        if (!string.IsNullOrWhiteSpace(baseAddress))
            _baseUrl = baseAddress;
    }

    public Ratchet(string baseAddress = null)
    {
        _factory = new WebApplicationFactory<TSetup>();
        _testHostClient = _factory.CreateClient();
        if (!string.IsNullOrWhiteSpace(baseAddress))
            _baseUrl = baseAddress;
    }

    private WebApplicationFactory<TSetup> CreateFactory(Action<WebHostBuilder> configure)
    {
        return new WebApplicationFactory<TSetup>().WithWebHostBuilder(configure);
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
            _context = await _browser.NewContextAsync();
            _page = await _context.NewPageAsync();

            // Setup route interception for in-memory TestHost
            await SetupRouteInterceptionAsync();

            // Setup console logging
            _page.Console += (_, msg) => OnConsoleLog?.Invoke(msg.Text);

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task SetupRouteInterceptionAsync()
    {
        await _page.RouteAsync("**/*", async route =>
        {
            var request = route.Request;
            var url = request.Url;

            // Only intercept our app URLs
            if (!url.StartsWith(_baseUrl))
            {
                await route.ContinueAsync();
                return;
            }

            try
            {
                // Forward to in-memory TestHost
                var httpRequest = new HttpRequestMessage
                {
                    Method = new HttpMethod(request.Method),
                    RequestUri = new Uri(url)
                };

                foreach (var header in request.Headers.Where(h => !IsRestrictedHeader(h.Key)))
                {
                    httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                if (request.PostData != null)
                {
                    httpRequest.Content = new StringContent(request.PostData);
                }

                var response = await _testHostClient.SendAsync(httpRequest);

                await route.FulfillAsync(new()
                {
                    Status = (int)response.StatusCode,
                    Headers = ConvertHeaders(response),
                    BodyBytes = await response.Content.ReadAsByteArrayAsync()
                });
            }
            catch
            {
                await route.ContinueAsync();
            }
        });
    }

    private static bool IsRestrictedHeader(string name) =>
        new[] { "Host", "Content-Length", "Transfer-Encoding", "Connection" }
            .Contains(name, StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, string> ConvertHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>();
        foreach (var h in response.Headers)
            headers[h.Key] = string.Join(", ", h.Value);
        if (response.Content?.Headers != null)
            foreach (var h in response.Content.Headers)
                headers[h.Key] = string.Join(", ", h.Value);
        return headers;
    }

    public async Task<PlaywrightDocument> OpenUrl(string path)
    {
        await EnsureInitializedAsync();

        if (!Uri.TryCreate(path, UriKind.Absolute, out _))
        {
            path = new Uri(new Uri(_baseUrl), path).AbsoluteUri;
        }

        await _page.GotoAsync(path);
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        return new PlaywrightDocument(_page);
    }

    public void ExecuteJavaScript(string script)
    {
        _page.EvaluateAsync(script).GetAwaiter().GetResult();
    }

    public async void FillInput(string query, string text)
    {
        await EnsureInitializedAsync();
        await _page.FillAsync(query, text);
    }

    public async void ElementClick(string query)
    {
        await EnsureInitializedAsync();
        await _page.ClickAsync(query);
    }

    public async void FillTextArea(string query, string text)
    {
        await EnsureInitializedAsync();
        await _page.FillAsync(query, text);
    }

    public async Task<PlaywrightDocument> WaitDocumentLoad()
    {
        await EnsureInitializedAsync();
        await _page.WaitForLoadStateAsync(LoadState.Load);
        return new PlaywrightDocument(_page);
    }

    public async Task<string> WaitNextConsoleLog()
    {
        await EnsureInitializedAsync();
        var tcs = new TaskCompletionSource<string>();

        void Handler(object obj)
        {
            tcs.TrySetResult(obj?.ToString());
            OnConsoleLog -= Handler;
        }

        OnConsoleLog += Handler;
        return await tcs.Task;
    }

    public async Task<string> WaitNextAlert()
    {
        await EnsureInitializedAsync();
        var tcs = new TaskCompletionSource<string>();

        void Handler(object sender, IDialog dialog)
        {
            tcs.TrySetResult(dialog.Message);
            dialog.AcceptAsync().GetAwaiter().GetResult();
        }

        _page.Dialog += Handler;
        var result = await tcs.Task;
        _page.Dialog -= Handler;

        return result;
    }

    public async Task<IElementHandle> WaitId(string id, int timeout = 0)
    {
        await EnsureInitializedAsync();
        timeout = timeout == 0 ? DefaultTimeout : timeout;
        return await _page.WaitForSelectorAsync($"#{id}", new() { Timeout = timeout });
    }

    public async Task WaitDesappearingOfId(string id, int timeout = 0)
    {
        await EnsureInitializedAsync();
        timeout = timeout == 0 ? DefaultTimeout : timeout;
        await _page.WaitForSelectorAsync($"#{id}", new()
        {
            State = WaitForSelectorState.Hidden,
            Timeout = timeout
        });
    }

    public async Task<IElementHandle> FirstElement(string query)
    {
        await EnsureInitializedAsync();
        return await _page.QuerySelectorAsync(query);
    }

    public async Task<IReadOnlyList<IElementHandle>> WaitSelector(string query, int timeout = 0)
    {
        await EnsureInitializedAsync();
        timeout = timeout == 0 ? DefaultTimeout : timeout;
        await _page.WaitForSelectorAsync(query, new() { Timeout = timeout });
        return await _page.QuerySelectorAllAsync(query);
    }

    public Task WaitDocumentChanged(string query, int timeout = 0)
    {
        // Playwright handles DOM changes automatically
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<IElementHandle>> Get(string query)
    {
        await EnsureInitializedAsync();
        return await _page.QuerySelectorAllAsync(query);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_context != null) await _context.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
        _testHostClient?.Dispose();
        if (_factory != null) await _factory.DisposeAsync();
        _initLock?.Dispose();
    }
}

/// <summary>
/// Wrapper for Playwright page to match original Document API
/// </summary>
public class PlaywrightDocument
{
    private readonly IPage _page;

    public PlaywrightDocument(IPage page)
    {
        _page = page;
    }

    public string TextContent => _page.TextContentAsync("body").GetAwaiter().GetResult();

    public async Task<string> GetTextContentAsync() => await _page.TextContentAsync("body");
}
