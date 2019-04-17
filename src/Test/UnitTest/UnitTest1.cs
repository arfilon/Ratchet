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
            var engine = new Ratchet<WebApplication.Startup>();

            await engine.OpenUrl("/home/About");

            var Document = await engine.WaitDocumentLoad();

            var t = Document.TextContent;
            var c = engine.WaitNextConsoleLog();
            engine.ExecuteJavaScript("console.log('mmmm');");

            TestContext.WriteLine("con: " + await c);
        }



    }
}
