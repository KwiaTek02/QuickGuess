namespace Frontend.Services
{
    public interface IAuthService
    {
        Task<AuthResult> Login(string email, string password);
        Task<AuthResult> Register(string username, string email, string password);
        Task Logout();
        Task<AuthResult> GoogleLogin(string idToken);
    }

    public class AuthResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }

        public static AuthResult Ok() => new() { Success = true };
        public static AuthResult Fail(string? err) => new() { Success = false, Error = err };
    }
}