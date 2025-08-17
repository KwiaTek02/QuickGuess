namespace Frontend.Services
{
    public interface IAuthService
    {
        Task<bool> Login(string email, string password);
        Task<bool> Register(string username, string email, string password);
        Task Logout();
        Task<bool> GoogleLogin(string idToken);
    }
}
