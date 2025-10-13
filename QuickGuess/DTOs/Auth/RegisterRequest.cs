using System.ComponentModel.DataAnnotations;
using QuickGuess.Validation;

namespace QuickGuess.DTOs.Auth
{
    public class RegisterRequest
    {
        [Required, MinLength(3), MaxLength(32)]
        [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Dozwolone są litery, cyfry, kropki, myślniki i podkreślenia.")]
        public string Username { get; set; } = null!;

        [Required, EmailAddress, MaxLength(254)]
        public string Email { get; set; } = null!;

        [Required, StrongPassword]
        public string Password { get; set; } = null!;
    }
}