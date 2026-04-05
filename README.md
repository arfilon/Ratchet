# Ratchet

[![Build and Test](https://github.com/arfilon/Ratchet/actions/workflows/build.yml/badge.svg)](https://github.com/arfilon/Ratchet/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Arfilon.Ratchet.svg)](https://www.nuget.org/packages/Arfilon.Ratchet/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A lightweight browser automation library for ASP.NET Core testing built on top of Playwright. Test your web applications with real browser execution while keeping everything in-memory using ASP.NET Core's TestHost—no Kestrel server required!

## Features

- **In-Memory Testing** - Uses ASP.NET Core TestHost for fast, isolated tests without spinning up Kestrel
- **Real Browser Execution** - Powered by Playwright for authentic browser automation (Chromium, Firefox, WebKit)
- **JavaScript Support** - Execute and test client-side JavaScript in a real browser environment
- **Route Interception** - Automatically intercepts localhost requests and forwards them to in-memory TestHost
- **Screenshot Debugging** - Capture screenshots of your application state for debugging failing tests
- **Console Log Capture** - Monitor and assert on browser console messages
- **No External Dependencies** - Everything runs in a single process without external web servers or browsers

## Installation

Install the package via NuGet:

```bash
dotnet add package Arfilon.Ratchet
```

Or via Package Manager Console:

```powershell
Install-Package Arfilon.Ratchet
```

## Requirements

- .NET 8.0 or later
- Compatible with MSTest, xUnit, NUnit, or any testing framework

## Quick Start

Here's a simple example testing a login flow:

```csharp
using Arfilon.Ratchet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class LoginTests
{
    [TestMethod]
    public async Task Login_WithValidCredentials_Succeeds()
    {
        // Arrange - Create a Ratchet instance with your Startup class
        var browser = new Ratchet<WebApplication.Startup>();

        // Act - Navigate to login page
        await browser.OpenUrl("/test/");

        // Fill in login form
        browser.FillInput("#txtUsername", "Admin");
        browser.FillInput("#txtPassword", "P@ssw0rd");
        browser.ElementClick("#btn");

        // Wait for navigation
        await browser.WaitDocumentLoad();

        // Assert - Verify successful login
        var heading = await browser.WaitSelector("h2");
        var headingText = await heading.First().InnerHTMLAsync();
        Assert.AreEqual("Edit", headingText.Trim());

        // Cleanup
        await browser.DisposeAsync();
    }
}
```

## Usage Examples

### Testing JavaScript Execution

```csharp
[TestMethod]
public async Task CanExecuteJavaScript()
{
    var browser = new Ratchet<WebApplication.Startup>();

    await browser.OpenUrl("/home/About");
    await browser.WaitDocumentLoad();

    // Execute JavaScript and capture console output
    var consoleTask = browser.WaitNextConsoleLog();
    browser.ExecuteJavaScript("console.log('Hello World');");

    var message = await consoleTask;
    Assert.AreEqual("Hello World", message);

    await browser.DisposeAsync();
}
```

### Taking Screenshots for Debugging

```csharp
[TestMethod]
public async Task TakeScreenshot_ForDebugging()
{
    var browser = new Ratchet<WebApplication.Startup>();

    await browser.OpenUrl("/test/");
    browser.FillInput("#txtUsername", "Admin");
    browser.FillInput("#txtPassword", "P@ssw0rd");
    browser.ElementClick("#btn");

    // Take a screenshot before assertion
    var screenshotPath = await browser.TakeScreenshot("login-debug.png");
    Console.WriteLine($"Screenshot saved to: {screenshotPath}");

    await browser.WaitDocumentLoad();

    await browser.DisposeAsync();
}
```

### Custom Configuration

You can customize the ASP.NET Core configuration:

```csharp
// With configuration builder
var browser = new Ratchet<Startup>((context, config) =>
{
    config.AddInMemoryCollection(new Dictionary<string, string>
    {
        ["Setting:Key"] = "Value"
    });
});

// With WebHostBuilder
var browser = new Ratchet<Startup>(builder =>
{
    builder.ConfigureServices(services =>
    {
        // Customize services
    });
});
```

### Using with Playwright Directly

For advanced scenarios, you can also use the underlying Playwright provider:

```csharp
[TestMethod]
public async Task AdvancedPlaywrightUsage()
{
    var provider = new PlaywrightTestHostProvider<WebApplication.Startup>();
    var playwright = await Playwright.CreateAsync();
    var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
    var page = await browser.NewPageAsync();

    // Setup route interception
    await provider.SetupRouteInterceptionAsync(page);

    // Now use Playwright's full API
    await page.GotoAsync("http://localhost/test/");
    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

    // Cleanup
    await browser.CloseAsync();
    playwright.Dispose();
    await provider.DisposeAsync();
}
```

## API Reference

### Constructor Options

```csharp
// Default constructor
new Ratchet<TStartup>()

// With configuration delegate
new Ratchet<TStartup>((context, config) => { /* configure */ })

// With WebHostBuilder delegate
new Ratchet<TStartup>(builder => { /* configure */ })

// With custom base address
new Ratchet<TStartup>(baseAddress: "http://localhost:5000")
```

### Navigation Methods

- `OpenUrl(string path)` - Navigate to a URL (relative or absolute)
- `WaitDocumentLoad()` - Wait for the document to fully load

### Element Interaction

- `FillInput(string query, string text)` - Fill an input field
- `FillTextArea(string query, string text)` - Fill a textarea
- `ElementClick(string query)` - Click an element

### Element Querying

- `WaitId(string id, int timeout = 0)` - Wait for element with ID
- `WaitSelector(string query, int timeout = 0)` - Wait for CSS selector
- `WaitDesappearingOfId(string id, int timeout = 0)` - Wait for element to disappear
- `FirstElement(string query)` - Get first matching element
- `Get(string query)` - Get all matching elements

### JavaScript & Console

- `ExecuteJavaScript(string script)` - Execute JavaScript code
- `WaitNextConsoleLog()` - Wait for and capture next console message
- `WaitNextAlert()` - Wait for and handle next alert dialog

### Debugging

- `TakeScreenshot(string path = null, bool fullPage = false)` - Capture page screenshot

### Configuration

- `DefaultTimeout` - Default timeout in milliseconds (default: 20000)
- `OnConsoleLog` - Event handler for console messages

## How It Works

Ratchet combines ASP.NET Core's TestHost with Playwright's browser automation:

1. **TestHost** - Your application runs in-memory using ASP.NET Core's TestHost (no Kestrel required)
2. **Playwright** - Provides a real Chromium browser instance for authentic testing
3. **Route Interception** - Playwright's route interception forwards all `localhost` requests to the in-memory TestHost
4. **Result** - You get real browser behavior (JavaScript, rendering, etc.) while keeping tests fast and isolated

This architecture provides:
- **Speed** - No network overhead, everything runs in-process
- **Isolation** - Each test gets its own application instance
- **Authenticity** - Real browser execution with JavaScript support
- **Simplicity** - No need to manage external browsers or servers

## CI/CD Integration

The project includes a GitHub Actions workflow (`.github/workflows/build.yml`) that:

- Builds the solution
- Runs all tests
- Packages the NuGet package
- Uploads artifacts

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Links

- **Repository**: https://github.com/arfilon/Ratchet
- **NuGet Package**: https://www.nuget.org/packages/Arfilon.Ratchet/
- **Issues**: https://github.com/arfilon/Ratchet/issues

## Credits

Built with:
- [Playwright](https://playwright.dev/) - Browser automation framework
- [ASP.NET Core](https://docs.microsoft.com/aspnet/core/) - Web framework
- [ASP.NET Core TestHost](https://docs.microsoft.com/aspnet/core/test/integration-tests) - In-memory testing
