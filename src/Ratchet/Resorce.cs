using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Knyaz.Optimus.ResourceProviders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace Arfilon.Ratchet
{
    internal class Resorce<TSetup> : IResourceProvider where TSetup : class
    {
        private HttpClient client;
        private CookieContainer cookies;

        public string BaseAddress { get; }

        public Resorce()
        {

            client = new Microsoft.AspNetCore.TestHost.TestServer(new WebHostBuilder().UseStartup<TSetup>()).CreateClient();

            cookies = new System.Net.CookieContainer();
            this.BaseAddress = client.BaseAddress.OriginalString;
        }

        public async Task<IResource> SendRequestAsync(Request request)
        {
            var m = new HttpRequestMessage();
            m.Method = new HttpMethod(request.Method);
            if (request.Data != null)
                m.Content = new StringContent(Encoding.UTF8.GetString( request.Data),Encoding.UTF8, "application/x-www-form-urlencoded");
           
            var ch = cookies.GetCookieHeader(request.Url);
            m.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3");
            m.Headers.Add("Accept-Language", "en-US,en");
            m.Headers.Add("Host", request.Url.Host);
            m.Headers.Add("Origin", request.Url.AbsoluteUri.Substring(0, request.Url.AbsoluteUri.IndexOf(request.Url.AbsolutePath)));
            m.Headers.Add("Referer", request.Url.OriginalString);


            if (!string.IsNullOrWhiteSpace(ch))
                m.Headers.Add("cookie", ch);
            
            foreach (var h in request.Headers)
                m.Headers.Add(h.Key, h.Value);


            m.RequestUri = request.Url;

            HttpClient httpClient  =request.Url.OriginalString.StartsWith(BaseAddress)? client: new HttpClient();

            HttpResponseMessage resp = await httpClient.SendAsync(m);

            foreach (var k in resp.Headers.Where(t => t.Key.ToLower() == "set-cookie").SelectMany(t => t.Value))
            {
                cookies.SetCookies( request.Url,k);
            }
            var data = await resp.Content.ReadAsByteArrayAsync();
            var sr = new StreamReader(new MemoryStream(data));
            var text = await sr.ReadToEndAsync();
            return new TestResorce(resp.Content.Headers.ContentType?.MediaType ?? "text/html", new MemoryStream(data));
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