using Arfilon.Ratchet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest;

[TestClass]
public class ScreenshotTest
{
    [TestMethod]
    public async Task TakeScreenshot_WithDefaultPath()
    {
        var browser = new Ratchet<WebApplication.Startup>();

        await browser.OpenUrl("/home/About");

        // Take screenshot with auto-generated filename
        var screenshotPath = await browser.TakeScreenshot();

        Console.WriteLine($"Screenshot saved to: {screenshotPath}");

        // Verify file exists
        Assert.IsTrue(File.Exists(screenshotPath), "Screenshot file should exist");

        // Cleanup
        File.Delete(screenshotPath);
        await browser.DisposeAsync();
    }

    [TestMethod]
    public async Task TakeScreenshot_WithCustomPath()
    {
        var browser = new Ratchet<WebApplication.Startup>();

        await browser.OpenUrl("/home/About");

        // Take screenshot with custom filename
        var screenshotPath = await browser.TakeScreenshot("test-output/about-page.png");

        Console.WriteLine($"Screenshot saved to: {screenshotPath}");

        // Verify file exists
        Assert.IsTrue(File.Exists(screenshotPath), "Screenshot file should exist");

        // Cleanup
        if (Directory.Exists("test-output"))
            Directory.Delete("test-output", true);

        await browser.DisposeAsync();
    }

    [TestMethod]
    public async Task TakeScreenshot_FullPage()
    {
        var browser = new Ratchet<WebApplication.Startup>();

        await browser.OpenUrl("/");

        // Take full page screenshot (captures entire scrollable page)
        var screenshotPath = await browser.TakeScreenshot("full-page.png", fullPage: true);

        Console.WriteLine($"Full page screenshot saved to: {screenshotPath}");

        // Verify file exists
        Assert.IsTrue(File.Exists(screenshotPath), "Screenshot file should exist");

        // Cleanup
        File.Delete(screenshotPath);
        await browser.DisposeAsync();
    }
}
