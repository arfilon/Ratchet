using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest;

/// <summary>
/// Demonstrates using Playwright with in-memory ASP.NET Core TestHost.
/// No Kestrel required - all requests are intercepted and forwarded to TestServer!
/// </summary>
[TestClass]
public class PlaywrightTests
{
    private PlaywrightTestHostProvider<WebApplication.Startup>? _provider;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;

    public TestContext? TestContext { get; set; }

    [TestInitialize]
    public async Task Setup()
    {
        // Create in-memory TestHost provider
        _provider = new PlaywrightTestHostProvider<WebApplication.Startup>();

        // Initialize Playwright
        _playwright = await Playwright.CreateAsync();

        // Launch browser (headless by default)
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            // Headless = false, // Uncomment to see the browser
            // SlowMo = 1000 // Uncomment to slow down actions for debugging
        });

        // Create new page
        _page = await _browser.NewPageAsync();

        // Setup route interception - this is the magic! ✨
        // All localhost requests will be forwarded to in-memory TestHost
        await _provider.SetupRouteInterceptionAsync(_page);
    }

    [TestMethod]
    public async Task Login_WithPlaywright_UsingInMemoryTestHost()
    {
        // Navigate to app (intercepted and served from TestHost!)
        await _page!.GotoAsync("http://localhost/test/");

        // Wait for page to load
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Fill login form
        await _page.FillAsync("#txtUsername", "Admin");
        await _page.FillAsync("#txtPassword", "P@ssw0rd");

        // Click submit button
        await _page.ClickAsync("#btn");

        // Wait for navigation (also served from TestHost!)
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify we're on the correct page
        var heading = await _page.TextContentAsync("h2");
        Assert.AreEqual("Edit", heading?.Trim());

        TestContext?.WriteLine($"✅ Login successful! Heading: {heading}");
    }

    [TestMethod]
    public async Task JavaScriptExecution_WithPlaywright()
    {
        await _page!.GotoAsync("http://localhost/home/About");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Execute JavaScript in real browser
        var result = await _page.EvaluateAsync<string>("() => { console.log('Hello from Playwright!'); return 'Done'; }");

        Assert.AreEqual("Done", result);
        TestContext?.WriteLine($"✅ JavaScript executed: {result}");
    }

    [TestMethod]
    public async Task ConfirmDialog_Accept()
    {
        await _page!.GotoAsync("http://localhost/test/");

        // Setup dialog handler BEFORE triggering the dialog
        string? dialogMessage = null;
        DialogType? dialogType = null;

        _page.Dialog += async (_, dialog) =>
        {
            dialogMessage = dialog.Message;
            dialogType = dialog.Type;
            TestContext?.WriteLine($"📢 Dialog shown: [{dialog.Type}] {dialog.Message}");

            // Accept the dialog (click OK)
            await dialog.AcceptAsync();
        };

        // Trigger an action that shows a confirmation dialog
        // (You'll need to add a button that calls confirm() in your app)
        // await _page.ClickAsync("#confirmBtn");

        // For now, inject a test script
        await _page.EvaluateAsync(@"() => {
            const result = confirm('Are you sure?');
            document.body.setAttribute('data-confirm-result', result.toString());
        }");

        // Verify dialog was shown and accepted
        Assert.AreEqual(DialogType.Confirm, dialogType);
        Assert.AreEqual("Are you sure?", dialogMessage);

        var confirmResult = await _page.GetAttributeAsync("body", "data-confirm-result");
        Assert.AreEqual("true", confirmResult);

        TestContext?.WriteLine("✅ Confirmation dialog accepted");
    }

    [TestMethod]
    public async Task ConfirmDialog_Dismiss()
    {
        await _page!.GotoAsync("http://localhost/test/");

        bool dialogShown = false;

        _page.Dialog += async (_, dialog) =>
        {
            dialogShown = true;
            TestContext?.WriteLine($"📢 Dialog shown: {dialog.Message}");

            // Dismiss the dialog (click Cancel)
            await dialog.DismissAsync();
        };

        await _page.EvaluateAsync(@"() => {
            const result = confirm('Delete everything?');
            document.body.setAttribute('data-confirm-result', result.toString());
        }");

        Assert.IsTrue(dialogShown, "Dialog should have been shown");

        var confirmResult = await _page.GetAttributeAsync("body", "data-confirm-result");
        Assert.AreEqual("false", confirmResult);

        TestContext?.WriteLine("✅ Confirmation dialog dismissed");
    }

    [TestMethod]
    public async Task WaitForElement_AutoWaiting()
    {
        await _page!.GotoAsync("http://localhost/test/");

        // Playwright automatically waits for elements!
        // No manual delays or polling needed
        var usernameInput = await _page.WaitForSelectorAsync("#txtUsername");
        Assert.IsNotNull(usernameInput);

        // Check if element is visible
        var isVisible = await usernameInput.IsVisibleAsync();
        Assert.IsTrue(isVisible);

        TestContext?.WriteLine("✅ Element found and visible");
    }

    [TestMethod]
    public async Task NetworkRequests_AreIntercepted()
    {
        var requestUrls = new List<string>();

        // Monitor all requests
        _page!.Request += (_, request) =>
        {
            requestUrls.Add(request.Url);
            TestContext?.WriteLine($"🌐 Request: {request.Method} {request.Url}");
        };

        await _page.GotoAsync("http://localhost/home/About");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify localhost requests were made (and intercepted!)
        Assert.IsTrue(requestUrls.Any(u => u.StartsWith("http://localhost")));

        TestContext?.WriteLine($"✅ Intercepted {requestUrls.Count} requests");
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_page != null)
        {
            await _page.CloseAsync();
        }

        if (_browser != null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();

        if (_provider != null)
        {
            await _provider.DisposeAsync();
        }
    }
}
