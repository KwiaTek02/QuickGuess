using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Frontend.Services
{
    public class TokenGuard
    {
        private readonly IJSRuntime _js;
        private readonly NavigationManager _nav;

        public TokenGuard(IJSRuntime js, NavigationManager nav)
        {
            _js = js; _nav = nav;
        }

        public async Task<bool> EnsureValidTokenOrRedirect(string? returnUrl = null)
        {
            var token = await _js.InvokeAsync<string>("localStorage.getItem", "authToken");

            if (string.IsNullOrWhiteSpace(token) || IsExpired(token))
            {
                const string msg = "Twoja sesja wygasła. Zaloguj się ponownie.";

                await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
                await _js.InvokeVoidAsync("localStorage.removeItem", "username");
                await _js.InvokeVoidAsync("localStorage.removeItem", "email");
                await _js.InvokeVoidAsync("localStorage.removeItem", "publicId");

                try { await _js.InvokeVoidAsync("tg.showNotice", msg); }
                catch { await _js.InvokeVoidAsync("alert", msg); }

                await _js.InvokeVoidAsync("sessionStorage.setItem", "flash", msg);

                var target = "/login";
                if (!string.IsNullOrEmpty(returnUrl))
                    target += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";

                _nav.NavigateTo(target, forceLoad: true);
                return false;
            }

            return true;
        }
        public static bool IsExpired(string jwt)
        {
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length < 2) return true;

                var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
                using var doc = JsonDocument.Parse(payloadJson);

                if (!doc.RootElement.TryGetProperty("exp", out var expEl)) return true;

                var exp = expEl.GetInt64(); 
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return exp <= now;
            }
            catch
            {
                return true; 
            }
        }

        private static byte[] Base64UrlDecode(string input)
        {
            string s = input.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }
    }
}
