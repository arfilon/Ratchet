using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Knyaz.Optimus;
using Knyaz.Optimus.Dom;
using Knyaz.Optimus.Dom.Elements;
using Knyaz.Optimus.Dom.Interfaces;
using Knyaz.Optimus.ResourceProviders;
using Knyaz.Optimus.Dom.Css;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Arfilon.Ratchet
{
    public class Ratchet<TSetup> : IDisposable where TSetup : class
    {
        public int DefaultTimeout = 20000;
        private Resorce<TSetup> resourceProvider;
        private Engine engine;

        public event Action<object> OnConsoleLog;

        public Ratchet(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate,string baseAddress = null) : this(new Resorce<TSetup>(configureDelegate,baseAddress))
        {
        }
        private Ratchet(Resorce<TSetup> resourceProvider)
        {
            this.resourceProvider = resourceProvider;
            engine = new Knyaz.Optimus.Engine(resourceProvider);
            engine.Console.OnLog += Console_OnLog;
            engine.OnRequest += Engine_OnRequest;
            engine.DocumentChanged += Engine_DocumentChanged;
        }
        public Ratchet() : this(new Resorce<TSetup>())
        {
        }

        private void Engine_DocumentChanged()
        {
            foreach (var f in engine.Document.Forms)
            {
                f.OnSubmit -= Form_DefaultOnSubmit;
                f.OnSubmit += Form_DefaultOnSubmit;
            }
        }

        private void Form_DefaultOnSubmit(Knyaz.Optimus.Dom.Events.Event obj)
        {

        }

        private void Engine_OnRequest(Request obj)
        {
            obj.Headers.Add("Accept-Language", "en-US");
        }

        private void Console_OnLog(object obj)
        {
            OnConsoleLog?.Invoke(obj);
        }

        public void ExecuteJavaScript(string script)
        {
            engine.ScriptExecutor.Execute("text/javascript", script);
        }

        //
        // Summary:
        //     Creates new Knyaz.Optimus.Engine.Document and loads it from specified path (http
        //     or file).
        //
        // Parameters:
        //   path:
        //     The string which represents Uri of the document to be loaded.

        public async Task<Document> OpenUrl(string path)
        {

            if (!System.Uri.TryCreate(path, UriKind.Absolute, out Uri url))
            {
                var baseAddress = resourceProvider.BaseAddress;
                path = new Uri(new Uri(baseAddress), path).AbsoluteUri;
            }
            var p = await engine.OpenUrl(path);
            Engine_DocumentChanged();
            return p.Document;
        }



        /// <summary>
        /// Emulate entering text by user into input textbox.
        /// </summary>
        public void FillInput(string query, string text)
        {
            var input = Get<HtmlInputElement>(query).Single();
            input.Value = text;
            var evt = input.OwnerDocument.CreateEvent("Event");
            evt.InitEvent("change", false, false);
            input.DispatchEvent(evt);

        }

        /// <summary>
		/// Emulate entering text by user into input textbox.
		/// </summary>
		public void ElementClick(string query)
        {
            var input = Get<HtmlElement>(query).Single();
            input.OnClick += Input_OnClick;
            //ExecuteJavaScript($@"document.getElementById('{query}').click();");
            input.Click();
            //var evt = input.OwnerDocument.CreateEvent("Event");
            //evt.InitEvent("click", false, false);
            //input.DispatchEvent(evt);
        }

        private bool? Input_OnClick(Knyaz.Optimus.Dom.Events.Event arg)
        {
            return null;
        }

        /// <summary>
        /// Emulate entering text by user into input textbox.
        /// </summary>
        public void FillTextArea(string query, string text)
        {

            var input = Get<HtmlTextAreaElement>(query).Single();
            input.Value = text;
            var evt = input.OwnerDocument.CreateEvent("Event");
            evt.InitEvent("change", false, false);
            input.DispatchEvent(evt);
        }


        /// <summary>
        /// Wait for the loading of document.
        /// </summary>
        public Task<Document> WaitDocumentLoad()
        {
            var taskb = new TaskCompletionSource<Document>();

            if (engine.Document.ReadyState != DocumentReadyStates.Loading)
                taskb.SetResult(engine.Document);


            Action<IDocument> handler = null;
            Action<Node, Exception> Errorhandler = null;
            handler = document =>
            {
                taskb.SetResult((Document)document);
                engine.Document.DomContentLoaded -= handler;
                engine.Document.OnNodeException -= Errorhandler;
            };
            Errorhandler = (nod, exception) =>
            {
                taskb.SetException(exception);
                engine.Document.DomContentLoaded -= handler;
                engine.Document.OnNodeException -= Errorhandler;
            };

            engine.Document.DomContentLoaded += handler;
            engine.Document.OnNodeException += Errorhandler;

            return taskb.Task;

        }
        public Task<string> WaitNextConsoleLog()
        {
            var taskb = new TaskCompletionSource<string>();
            Action<object> handler = null;
            handler = text =>
            {
                taskb.SetResult((string)text);
                engine.Console.OnLog -= handler;
            };
            engine.Console.OnLog += handler;

            return taskb.Task;

        }
        public Task<string> WaitNextAlert()
        {
            var taskb = new TaskCompletionSource<string>();
            Action<object> handler = null;
            handler = text =>
            {
                taskb.SetResult((string)text);
                engine.Window.OnAlert -= handler;
            };
            engine.Window.OnAlert += handler;

            return taskb.Task;
        }

        /// <summary>
        /// Blocks the execution of the current thread until an item with the specified ID appears in the document.
        /// </summary>
        /// <param name="engine">The engine with the document to wait in.</param>
        /// <param name="id">The identifier to be awaited.</param>
        /// <returns>Element with specified Id, <c>null</c> if the element with the specified identifier has not appeared in the document for the default timeout.</returns>
        public Task<Element> WaitId(string id)
        {
            return WaitId(id, DefaultTimeout);
        }

        /// <summary>
        /// Blocks the execution of the current thread until an item with the specified ID appears in the document.
        /// </summary>
        /// <param name="engine">Document onwer.</param>
        /// <param name="id">Id of element waiting for.</param>
        /// <param name="timeout">The time to wait in milliseconds</param>
        /// <returns>Element with specified Id, <c>null</c> if the element with the specified identifier has not appeared in the document for a given time.</returns>
        public async Task<Element> WaitId(string id, int timeout)
        {
            await WaitDocumentLoad();
            var timespan = 100;
            for (int i = 0; i < timeout / timespan; i++)
            {
                var doc = engine.Document;
                lock (doc)
                {
                    try
                    {
                        var elt = doc.GetElementById(id);
                        if (elt != null)
                            return elt;
                    }
                    catch
                    {
                        //catch 'collection was changed...'
                    }
                }

                await Task.Delay(timespan);
            }
            throw new TimeoutException();
        }

        /// <summary>
        /// Locks the current thread until the element with specified id disappears.
        /// </summary>
        /// <param name="id">Identifier of the item to be disappeared.</param>
        /// <returns>Element if found, <c>null</c> othervise.</returns>
        public Task WaitDesappearingOfId(string id)
        {
            return WaitDesappearingOfId(id, DefaultTimeout);
        }

        /// <summary>
        /// Locks the current thread until the element with specified id disappears.
        /// </summary>
        /// <param name="id">Identifier of the item to be disappeared.</param>
        /// <param name="timeout">The timeout</param>
        /// <returns>Element if found, <c>null</c> othervise.</returns>
        public async Task WaitDesappearingOfId(string id, int timeout)
        {
            var timespan = 100;
            for (int i = 0; i < timeout / timespan; i++)
            {
                var doc = engine.Document;
                lock (doc)
                {
                    var elt = doc.GetElementById(id);
                    if (elt == null)
                        return;
                }

                await Task.Delay(timespan);
            }
            throw new TimeoutException();
        }

        /// <summary>
        /// Search the first html element in document which satisfies specified selector.
        /// </summary>
        /// <returns>Found <see cref="HtmlElement"/> or <c>null</c>.</returns>
        public HtmlElement FirstElement(string query)
        {
            return engine.Document.QuerySelectorAll(query).OfType<HtmlElement>().First();
        }

        /// <summary>
        /// Freezes the current thread until at least one element that matches the query appears in the document.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="query"></param>
        /// <param name="timeout"></param>
        /// <returns>Matched elements.</returns>
        public async Task<IEnumerable<IElement>> WaitSelector(string query, int timeout = 0)
        {
            await WaitDocumentLoad();
            if (timeout == 0)
                timeout = DefaultTimeout;

            var timespan = 100;
            for (int i = 0; i < timeout / timespan; i++)
            {
                try
                {
                    var elt = engine.Document.QuerySelectorAll(query);
                    if (elt != null)
                        return elt;
                }
                catch
                {
                }

                await Task.Delay(timespan);
            }

            throw new TimeoutException();
        }
        public Task WaitDocumentChanged(string query, int timeout = 0)
        {
            var taskb = new TaskCompletionSource<object>();
            Action handler = null;
            handler = () =>
            {
                taskb.SetResult(null);
                engine.DocumentChanged -= handler;
            };
            engine.DocumentChanged += handler;

            return taskb.Task;
        }

        /// <summary>
        /// Alias for QuerySelectorAll method with element types filtration.
        /// </summary>
        public IEnumerable<T> Get<T>(string query) where T : IElement
        {
            return engine.Document.QuerySelectorAll(query).OfType<T>();
        }
        public void Dispose()
        {
            engine.Dispose();
        }
        
    }
}
