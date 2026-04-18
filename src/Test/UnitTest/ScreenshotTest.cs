using Arfilon.Ratchet;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace UnitTest;

[TestClass]
public class ScreenshotTest
{
    private const bool cleanup = false;
    private static TestContext testContext;
    private static string screenshotFolder;
    private static IBrowserContext appContext;
    private IPage browser;


    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        testContext = context;
        screenshotFolder = Path.Combine(context.TestRunDirectory, "screenshots");
        var application = Ratchet.Create<WebApplication.Startup>();
        appContext = await application.NewContextAsync(false);
    }
    [TestInitialize()]
    public async Task Initialize()
    {
        browser = await appContext.NewPageAsync();
    }

    [TestCleanup()]
    public async Task Cleanup()
    {
        await browser.CloseAsync();
    }

    [ClassCleanup()]
    public static async Task ClassCleanup()
    {
       await appContext.DisposeAsync();
    }

    [TestMethod]
    public async Task TakeScreenshot_WithCustomPath()
    {

        await browser.GotoAsync("/home/About");
        var screenshotPath = Path.Combine(screenshotFolder, "about-page.png");

        // Take screenshot with custom filename
        await browser.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = screenshotPath,
            FullPage = false
        });

        Console.WriteLine($"Screenshot saved to: {screenshotPath}");

        // Verify file exists
        Assert.IsTrue(File.Exists(screenshotPath), "Screenshot file should exist");

        // Cleanup
        if (cleanup && Directory.Exists("test-output"))
            Directory.Delete("test-output", true);
    }

    [TestMethod]
    public async Task TakeScreenshot_FullPage()
    {
        await browser.GotoAsync("/");

        var screenshotPath = Path.Combine(screenshotFolder, "about-page-full.png");

        // Take full page screenshot (captures entire scrollable page)
        await browser.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = screenshotPath,
            FullPage = true
        });

        Console.WriteLine($"Full page screenshot saved to: {screenshotPath}");

        // Verify file exists
        Assert.IsTrue(File.Exists(screenshotPath), "Screenshot file should exist");

        // Cleanup
        if (cleanup)
        {
            File.Delete(screenshotPath);
        }
    }

    [TestMethod]
    public async Task Login()
    {
        var b = Arfilon.Ratchet.Ratchet.Create<WebApplication.Startup>();

        await browser.GotoAsync("/test/");
        await browser.FillAsync("#txtUsername", "Admin");
        await browser.FillAsync("#txtPassword", "P@ssw0rd");
        await browser.ClickAsync("#btn");

        // Take screenshot before assertion for debugging
        var screenshotPath = Path.Combine(screenshotFolder, "login-test-debug.png");
        await browser.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = screenshotPath,
            FullPage = true
        });
        testContext.WriteLine($"Screenshot saved to: {screenshotPath}");

        var username = await browser.WaitForSelectorAsync("h2");
        var firstElement = await username.TextContentAsync();
        var innerHTML = firstElement;

        Assert.AreEqual("Edit", innerHTML.Trim());

        await b.DisposeAsync();
    }

    [TestMethod]
    public async Task Foo()
    {
        await browser.GotoAsync("/home/About");

        var Document = await browser.WaitForSelectorAsync("body");
        string? consoleMessage = null;
        void Handler(object? s,IConsoleMessage obj)
        {
            consoleMessage = obj.Text;
            browser.Console -= Handler;
        }

        browser.Console += Handler;
        await browser.EvaluateAsync("console.log('Hello World');");

      

        Assert.AreEqual("Hello World", consoleMessage);

    }
}
