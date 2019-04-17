using System;
using System.Linq;
using System.Threading.Tasks;
using Arfilon.Ratchet;
using Knyaz.Optimus;
using Knyaz.Optimus.ResourceProviders;
using Knyaz.Optimus.TestingTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{

    [TestClass]
    public class UnitTest1
    {
        public TestContext TestContext { get; set; }
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
        }



    }
}
