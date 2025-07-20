using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace Frontend.Services
{
    public class AuthorizedHttpClient
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;

        public AuthorizedHttpClient(HttpClient http, IJSRuntime js)
        {
            _http = http;
            _js = js;
        }

        public async Task<HttpClient> GetClientAsync()
        {
            var token = await _js.InvokeAsync<string>("localStorage.getItem", "authToken");

            _http.DefaultRequestHeaders.Authorization = null; // ważne: usuń stary nagłówek
            if (!string.IsNullOrWhiteSpace(token))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return _http;
        }
    }
}