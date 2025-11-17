# Ratchet


End-to-end testing framework for ASP.NET Core applications (.NET 8) in one process without web-server (like IIS or Kestrel) or browser (like Chrome or IE).

## c# Sample



```sh
var browser = new Ratchet<WebApplication.Startup>();

await browser.OpenUrl("/home/About");

var Document = await browser.WaitDocumentLoad();

var DocumentText = Document.TextContent;

var c = browser.WaitNextConsoleLog();

browser.ExecuteJavaScript("console.log('Hello World');");

TestContext.WriteLine("con: " + await c);
```
TestContext Output : Hello World 
            
