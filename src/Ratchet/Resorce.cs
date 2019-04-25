using System;
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
using Microsoft.Extensions.Configuration;

namespace Arfilon.Ratchet
{
    internal class Resorce<TSetup> : IResourceProvider where TSetup : class
    {
        private HttpClient client;
        private CookieContainer cookies;

        public string BaseAddress { get; }

        public Resorce(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate, string baseAddress = null) : this(
            new Microsoft.AspNetCore.TestHost.TestServer(new WebHostBuilder().ConfigureAppConfiguration(configureDelegate).UseStartup<TSetup>()).CreateClient(),
            baseAddress
            )
        {
        }
        public Resorce(Action<WebHostBuilder> configureDelegate, string baseAddress = null) : this(
            GetByConfig(configureDelegate),
            baseAddress
            )
        {
        }

        private static HttpClient GetByConfig(Action<WebHostBuilder> configureDelegate)
        {
            var b = new WebHostBuilder();
            configureDelegate(b);
            b.UseStartup<TSetup>();
            return new Microsoft.AspNetCore.TestHost.TestServer(b).CreateClient();
        }

        public Resorce(string baseAdress = null) : this(
            new Microsoft.AspNetCore.TestHost.TestServer(new WebHostBuilder().UseStartup<TSetup>()).CreateClient(),
            baseAdress
            )
        {
        }
        private Resorce(HttpClient client, string baseAddress = null)
        {
            this.client = client;
          
            cookies = new System.Net.CookieContainer();
            if (!string.IsNullOrWhiteSpace(baseAddress))
                client.BaseAddress = new Uri(baseAddress);
            this.BaseAddress = client.BaseAddress.OriginalString;
        }

        public async Task<IResource> SendRequestAsync(Request request)
        {
            var m = new HttpRequestMessage();
            m.Method = new HttpMethod(request.Method);
            m.Version = HttpVersion.Version11;

            if (request.Data != null)
                m.Content = new StringContent(Encoding.UTF8.GetString(request.Data), Encoding.UTF8, "application/x-www-form-urlencoded");

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

            HttpClient httpClient = null;
            if (request.Url.OriginalString.StartsWith(BaseAddress))
               httpClient = client;
            else
                httpClient = new HttpClient();

            HttpResponseMessage resp = await httpClient.SendAsync(m);

            foreach (var k in resp.Headers.Where(t => t.Key.ToLower() == "set-cookie").SelectMany(t => t.Value))
            {
                cookies.SetCookies(request.Url, k);
            }
            var data = await resp.Content.ReadAsByteArrayAsync();
            var sr = new StreamReader(new MemoryStream(data));
            var text = await sr.ReadToEndAsync();
            if (resp.StatusCode != HttpStatusCode.OK && string.IsNullOrWhiteSpace(text))
            {
                return new TestResorce(resp.Content.Headers.ContentType?.MediaType ?? "text/html", new MemoryStream(Encoding.UTF8.GetBytes($"Error: httpStatusCode {(int)resp.StatusCode }:{nameof(resp.StatusCode) }".ToCharArray())));
            }
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