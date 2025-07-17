using System.Security.Cryptography;

namespace QuickGuess.Services.Auth
{
    public static class TokenGenerator
    {
        public static string GenerateToken(int length = 64)
        {
            var bytes = RandomNumberGenerator.GetBytes(length);
            return Convert.ToBase64String(bytes);
        }
    }
}
