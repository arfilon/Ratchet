using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Knyaz.Optimus;
using Knyaz.Optimus.ResourceProviders;

namespace Arfilon.Ratchet
{
    public class Ratchet<TSetup> : Knyaz.Optimus.Engine where TSetup : class
    {
        public Ratchet() : base(new Resorce<TSetup>())
        {

        }

        //
        // Summary:
        //     Creates new Knyaz.Optimus.Engine.Document and loads it from specified path (http
        //     or file).
        //
        // Parameters:
        //   path:
        //     The string which represents Uri of the document to be loaded.

        public new Task<Page> OpenUrl(string path)
        {
            if (System.Uri.TryCreate(path,UriKind.Absolute,out Uri url))
                return base.OpenUrl(path);
            else
            {
                var baseAddress = ((Resorce<TSetup>)ResourceProvider).BaseAddress;
                return base.OpenUrl(new Uri(new Uri(baseAddress), path).AbsoluteUri);
            }
        }
    }
}
