namespace QuickGuess.Models.Settings
{
    public class JwtSettings
    {
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public string Secret { get; set; } = null!;
        public int TokenLifetimeMinutes { get; set; }
    }
}
