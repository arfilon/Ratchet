using Microsoft.Playwright;
using System.Text.Json;

namespace Arfilon.Ratchet
{
    internal class RatchetResponse : IAPIResponse
    {
        private HttpResponseMessage response;

        public RatchetResponse(HttpResponseMessage response)
        {
            this.response = response;
        }

        public Dictionary<string, string> Headers => throw new NotImplementedException();

        public IReadOnlyList<Header> HeadersArray => throw new NotImplementedException();

        public bool Ok => response.IsSuccessStatusCode;

        public int Status => (int) response.StatusCode;

        public string StatusText => response.ReasonPhrase;

        public string Url => response.RequestMessage?.RequestUri?.ToString() ?? string.Empty;

        public Task<byte[]> BodyAsync()
        {
            throw new NotImplementedException();
        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<JsonElement?> JsonAsync()
        {
            throw new NotImplementedException();
        }

        public Task<T?> JsonAsync<T>(JsonSerializerOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public Task<string> TextAsync()
        {
            throw new NotImplementedException();
        }
    }
}