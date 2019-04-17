# Ratchet


End to end test For asp.Net Core 2 in one Process without web-server (Like IIS or Kestrel) or browser (like chrome or IE).

## Sample



   ```sh
   
 var browser = new Ratchet<WebApplication.Startup>();

            await browser.OpenUrl("/home/About");

            var Document = await browser.WaitDocumentLoad();

            var t = Document.TextContent;
            var c = browser.WaitNextConsoleLog();
            browser.ExecuteJavaScript("console.log('Hello World');");

            TestContext.WriteLine("con: " + await c);

            // TestContext Output : Hello World 
            
            ```
