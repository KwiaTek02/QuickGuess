using System.ComponentModel.DataAnnotations.Schema;

namespace QuickGuess.Models
{
    [Table("email_verification_tokens")]
    public class EmailVerificationToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool Used { get; set; } = false;
    }
}
