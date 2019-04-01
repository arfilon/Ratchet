using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Knyaz.Optimus.ResourceProviders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace Arfilon.Ratchet
{
    internal class Resorce<TSetup> : IResourceProvider where TSetup :class
    {
        private HttpClient client;

        public string BaseAddress { get; }

        public Resorce()
        {

            client = new Microsoft.AspNetCore.TestHost.TestServer(new WebHostBuilder().UseStartup<TSetup>()).CreateClient();
            this.BaseAddress = client.BaseAddress.OriginalString;
        }

        public async Task<IResource> SendRequestAsync(Request request)
        {
            var m = new HttpRequestMessage();
            m.Method = new HttpMethod(request.Method);
            if (request.Data != null)
                m.Content = new ByteArrayContent(request.Data);
            foreach (var h in request.Headers)
                m.Headers.Add(h.Key, h.Value);

            m.RequestUri = request.Url;

            HttpResponseMessage resp = null;
            if (request.Url.OriginalString.StartsWith(BaseAddress))
            {
                resp = await client.SendAsync(m);
            }
            else
            {
                
                resp = await new HttpClient().SendAsync(m);

            }

            return new TestResorce(resp.Content.Headers.ContentType.MediaType, await resp.Content.ReadAsStreamAsync());
        }
    }

    class TestResorce : IResource
    {
        private string mediaType;
        private Stream stream;



        public TestResorce(string mediaType, Stream stream)
        {
            this.mediaType = mediaType;
            this.stream = stream;
        }

        public string Type => mediaType;

        public Stream Stream => stream;
    }
}