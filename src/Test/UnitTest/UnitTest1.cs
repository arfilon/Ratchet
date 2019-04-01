using System;
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
        public void Foo()
        {
            var engine = new E2EBrowser<WebApplication.Startup>();
            engine.Console.OnLog += Console_OnLog;

            //Open Html5Test site
            // var p = engine.OpenUrl("http://localhost/").Result;
            engine.OpenUrl("/home/About");

            //Wait until it finishes the test of browser and get DOM element with score value.
            //var tagWithValue = engine.WaitSelector("#score strong").FirstOrDefault();
            engine.WaitDocumentLoad();

            var t = engine.Document.TextContent;
            engine.ScriptExecutor.Execute("text/javascript", "console.log('mmmm');");

            //Show result
            //System.Console.WriteLine("Score: " + tagWithValue.InnerHTML);
            System.Console.ReadKey();
        }


        private void Console_OnLog(object obj)
        {
            TestContext.WriteLine(obj.ToString());
        }
    }
}
