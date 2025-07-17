using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickGuess.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")] 
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("username")]
        public string Username { get; set; } = null!;

        [Required]
        [Column("email")]
        public string Email { get; set; } = null!;

        [Column("email_verified")]
        public bool EmailVerified { get; set; } = false;

        [Column("password_hash")]
        public string? PasswordHash { get; set; }

        [Column("password_salt")]
        public string? PasswordSalt { get; set; }

        [Column("account_type")]
        public string AccountType { get; set; } = "user"; // 'user' or 'admin'

        [Column("provider")]
        public string Provider { get; set; } = "local";   // 'local' or 'google'

        [Column("google_id")]
        public string? GoogleId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
