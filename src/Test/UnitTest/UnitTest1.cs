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
        public async Task Login()
        {

            var b = new Arfilon.Ratchet.Ratchet<WebApplication.Startup>((w,c) => {
                
            });
            var p = await b.OpenUrl("/");
            b.FillInput("#txtUsername", "Admin");
            b.FillInput("#txtPassword", "P@ssw0rd");
            b.ElementClick("#btn");
            var p2 = await b.WaitDocumentLoad();

            var username = await b.WaitSelector(".user");
            Assert.AreEqual("Admin", username.Skip(1).First().InnerHTML.Trim());

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
        }



    }
}
