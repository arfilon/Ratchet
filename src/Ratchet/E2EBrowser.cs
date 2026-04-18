#nullable disable
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace Arfilon.Ratchet;

/// <summary>
/// Ratchet browser automation for ASP.NET Core testing, powered by Playwright.
/// Provides in-memory TestHost integration for fast, isolated testing.
/// </summary>
public class Ratchet : IDisposable, IAsyncDisposable
{
    public int DefaultTimeout = 20000;

    private readonly HttpClient _testHostClient;
    private string _baseUrl;

    private IPlaywright _playwright;
    private IBrowser _browser;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    // Constructors matching original API
    //public Ratchet(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
    //    : this(builder => builder.ConfigureAppConfiguration(configureDelegate)) { }


    //public static Ratchet Create<TSetup>(Action<IWebHostBuilder> webHostBuilderDelegate)
    //    : this(new WebApplicationFactory<TSetup>().WithWebHostBuilder(webHostBuilderDelegate)) { }

    public static Ratchet Create<TSetup>() where TSetup : class
    {
      return  Ratchet.Create(new WebApplicationFactory<TSetup>());
    }

    public static Ratchet Create<TSetup>(WebApplicationFactory<TSetup> factory) where TSetup : class
    {
        var _testHostClient = factory.CreateClient();
        return new Ratchet(_testHostClient);
    }

    public Ratchet (WebApplication webApplication)
    {
        //var s = webApplication.GetTestServer();
        var s = new TestServer( webApplication.Services);
        _testHostClient = s.CreateClient();
        webApplication.Run();
        _baseUrl = _testHostClient.BaseAddress.ToString();

    }
    private Ratchet (HttpClient httpClient) 
    {
        _testHostClient = httpClient;
        //_baseUrl = string.IsNullOrWhiteSpace(baseAddress)
        //    ? _testHostClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost"
        //    : baseAddress.TrimEnd('/');
        _baseUrl = _testHostClient.BaseAddress.ToString();
    }


    //private Ratchet(WebApplication webApplication)
    //{
    //    var x = 
    //    _factory = factory;
    //    _testHostClient = _factory.CreateClient();
    //    //_baseUrl = string.IsNullOrWhiteSpace(baseAddress)
    //    //    ? _testHostClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost"
    //    //    : baseAddress.TrimEnd('/');
    //    _baseUrl = _testHostClient.BaseAddress.ToString();
    //}

    public async Task<IBrowserContext> NewContextAsync(bool headless = true)
    {
        await CreateBrawser(headless);
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = _baseUrl,
            IgnoreHTTPSErrors = true
        });
        // Setup route interception for in-memory TestHost
        await SetupRouteInterceptionAsync(context);
        return context;

    }

    private async Task CreateBrawser(bool headless)
    {
        if (_browser != null) return;
        await _initLock.WaitAsync();
        if (_browser != null) return;
        try
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = headless });
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task SetupRouteInterceptionAsync(IBrowserContext context)
    {
        await context.RouteAsync(
            path => path.StartsWith(_baseUrl, StringComparison.OrdinalIgnoreCase),
            async route =>
        {
            try
            {
                var request = route.Request;

                // Forward to in-memory TestHost
                var httpRequest = new HttpRequestMessage
                {
                    Method = new HttpMethod(request.Method),
                    RequestUri = new Uri(request.Url)
                };
                httpRequest.Headers.Clear();
                var contentTypeHeaderName = "content-type";
                string[] butHeaders = [contentTypeHeaderName];
                foreach (var header in request.Headers.Where(t => !butHeaders.Contains(t.Key)))
                {
                    httpRequest.Headers.Add(header.Key, header.Value);
                }

                if (request.PostDataBuffer != null)
                {

                    var content = new System.Net.Http.ByteArrayContent(request.PostDataBuffer);
                    content.Headers.Add(contentTypeHeaderName, request.Headers[contentTypeHeaderName]);
                    httpRequest.Content = content;
                }

                var response = await _testHostClient.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead);
                RouteFulfillOptions routeFallbackOptions = new()
                {
                    //Body = clone.Body;
                    BodyBytes = await response.Content.ReadAsByteArrayAsync(),
                    //ContentType = clone.ContentType;
                    Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                    Status = (int)response.StatusCode
                    //Json = clone.Json;
                    //Path = clone.Path;
                    //Response = new RatchetResponse(response)
                    //Status = clone.Status;
                };
                throw new Exception($"Error processing request {route.Request.Url}");
                await route.FulfillAsync(routeFallbackOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing request {route.Request.Url}: {ex.Message}", ex);
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

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        //if (_page != null) await _page.CloseAsync();
        //if (_context != null) await _context.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
        _testHostClient?.Dispose();
        _initLock?.Dispose();
    }
}

