using Microsoft.JSInterop;
using System.Net;
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

        private sealed class ApiProblemDetails
        {
            public string? Title { get; set; }
            public Dictionary<string, string[]>? Errors { get; set; }
            public string? Detail { get; set; }
        }

        private static async Task<string> ExtractErrorAsync(HttpResponseMessage resp)
        {
            try
            {
                var pd = await resp.Content.ReadFromJsonAsync<ApiProblemDetails>();
                if (pd != null)
                {
                    if (pd.Errors != null && pd.Errors.Count > 0)
                        return string.Join(" ", pd.Errors.SelectMany(kv => kv.Value).Distinct());
                    if (!string.IsNullOrWhiteSpace(pd.Detail)) return pd.Detail!;
                    if (!string.IsNullOrWhiteSpace(pd.Title)) return pd.Title!;
                }
            }
            catch {  }

            var raw = await resp.Content.ReadAsStringAsync();

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (raw.Contains("verify your email", StringComparison.OrdinalIgnoreCase))
                    return "Najpierw zweryfikuj adres e-mail (sprawdź skrzynkę).";

                return "E-mail lub hasło są nieprawidłowe.";
            }

            return string.IsNullOrWhiteSpace(raw) ? $"Błąd {((int)resp.StatusCode)}" : raw;
        }

        public async Task<AuthResult> Login(string email, string password)
        {
            var response = await _http.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });

            if (!response.IsSuccessStatusCode)
                return AuthResult.Fail(await ExtractErrorAsync(response));

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result == null) return AuthResult.Fail("Nieoczekiwana odpowiedź serwera.");

            await _js.InvokeVoidAsync("localStorage.setItem", "authToken", result.Token);
            await _js.InvokeVoidAsync("localStorage.setItem", "authUser", result.Username);
            await _js.InvokeVoidAsync("localStorage.setItem", "authId", result.PublicId.ToString());

            _authState.NotifyAuthenticationChanged();
            return AuthResult.Ok();
        }

        public async Task<AuthResult> Register(string username, string email, string password)
        {
            var response = await _http.PostAsJsonAsync("/api/auth/register", new { Username = username, Email = email, Password = password });

            if (!response.IsSuccessStatusCode)
                return AuthResult.Fail(await ExtractErrorAsync(response));

            return AuthResult.Ok();
        }

        public async Task Logout()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
            await _js.InvokeVoidAsync("localStorage.removeItem", "authUser");
            await _js.InvokeVoidAsync("localStorage.removeItem", "authId");
            _authState.NotifyAuthenticationChanged();
        }

        private class AuthResponse
        {
            public string Token { get; set; } = "";
            public string Username { get; set; } = "";
            public string Email { get; set; } = "";
            public string Role { get; set; } = "";
            public Guid PublicId { get; set; }
        }

        public async Task<AuthResult> GoogleLogin(string idToken)
        {
            var response = await _http.PostAsJsonAsync("/api/auth/google-login", new { IdToken = idToken });
            if (!response.IsSuccessStatusCode)
                return AuthResult.Fail(await ExtractErrorAsync(response));

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result == null) return AuthResult.Fail("Nieoczekiwana odpowiedź serwera.");

            await _js.InvokeVoidAsync("localStorage.setItem", "authToken", result.Token);
            await _js.InvokeVoidAsync("localStorage.setItem", "authUser", result.Username);
            await _js.InvokeVoidAsync("localStorage.setItem", "authId", result.PublicId.ToString());

            _authState.NotifyAuthenticationChanged();
            return AuthResult.Ok();
        }
    }
}
