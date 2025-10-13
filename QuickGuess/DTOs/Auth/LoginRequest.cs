using System.ComponentModel.DataAnnotations;

namespace QuickGuess.DTOs.Auth
{
    public class LoginRequest
    {
        [Required, EmailAddress, MaxLength(254)]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }
}