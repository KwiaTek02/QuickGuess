using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace Frontend.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;
        private readonly AuthState _authState;

        public AuthService(HttpClient http, IJSRuntime js, AuthState authState)
        {
            _http = http;
            _js = js;
            _authState = authState;
        }

        public async Task<bool> Login(string email, string password)
        {
            var response = await _http.PostAsJsonAsync("/api/auth/login", new
            {
                Email = email,
                Password = password
            });

            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result == null) return false;

            await _js.InvokeVoidAsync("localStorage.setItem", "authToken", result.Token);
            await _js.InvokeVoidAsync("localStorage.setItem", "authUser", result.Username);

            _authState.NotifyAuthenticationChanged();
            return true;
        }

        public async Task<bool> Register(string username, string email, string password)
        {
            var response = await _http.PostAsJsonAsync("/api/auth/register", new
            {
                Username = username,
                Email = email,
                Password = password
            });

            return response.IsSuccessStatusCode;
        }

        public async Task Logout()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
            await _js.InvokeVoidAsync("localStorage.removeItem", "authUser");
            _authState.NotifyAuthenticationChanged();
        }

        private class AuthResponse
        {
            public string Token { get; set; } = "";
            public string Username { get; set; } = "";
            public string Email { get; set; } = "";
            public string Role { get; set; } = "";
        }
    }
}
