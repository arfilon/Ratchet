#nullable disable
using Arfilon.Ratchet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest;

[TestClass]
public class UnitTest1
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task Login()
    {
        var b = new Arfilon.Ratchet.Ratchet<WebApplication.Startup>((w, c) => { });

        var p = await b.OpenUrl("/test/");
        b.FillInput("#txtUsername", "Admin");
        b.FillInput("#txtPassword", "P@ssw0rd");
        b.ElementClick("#btn");
        var p2 = await b.WaitDocumentLoad();

        var username = await b.WaitSelector("h2");
        var firstElement = username.First();
        var innerHTML = await firstElement.InnerHTMLAsync();

        Assert.AreEqual("Edit", innerHTML.Trim());

        await b.DisposeAsync();
    }

    [TestMethod]
    public async Task Foo()
    {
        var browser = new Ratchet<WebApplication.Startup>();

        await browser.OpenUrl("/home/About");

        var Document = await browser.WaitDocumentLoad();

        var t = Document.TextContent;
        var c = browser.WaitNextConsoleLog();
        browser.ExecuteJavaScript("console.log('Hello World');");

        TestContext.WriteLine("con: " + await c);

        // TestContext Output : Hello World

        await browser.DisposeAsync();
    }
}
