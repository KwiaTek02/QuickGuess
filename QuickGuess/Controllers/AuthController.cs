using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickGuess.Data;
using QuickGuess.DTOs.Auth;
using QuickGuess.Models;
using QuickGuess.Services.Auth;
using System.Net;
using System.Net.Mail;

namespace QuickGuess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly JwtTokenGenerator _jwt;

        public AuthController(IConfiguration config)
        {
            _jwt = new JwtTokenGenerator(config);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request, [FromServices] ApplicationDbContext db)
        {
            if (await db.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest("Email already registered");

            PasswordHasher.CreateHash(request.Password, out string hash, out string salt);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = hash,
                PasswordSalt = salt,
                Provider = "local"
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Dodaj token weryfikacyjny
            var token = TokenGenerator.GenerateToken(32);
            var emailToken = new EmailVerificationToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
            db.EmailVerificationTokens.Add(emailToken);
            await db.SaveChangesAsync();

            await SendVerificationEmail(user.Email, token);

            return Ok("Registration successful. Check your email to verify your account.");
        }

        private async Task SendVerificationEmail(string email, string token)
        {
            var verifyUrl = $"https://localhost:7236/api/auth/verify-email?token={Uri.EscapeDataString(token)}";
            var body = $"<p>Welcome to QuickGuess!</p><p>Click to verify your account: <a href=\"{verifyUrl}\">Verify</a></p>";

            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential("your.email@gmail.com", "your_app_password")
            };

            var message = new MailMessage("your.email@gmail.com", email)
            {
                Subject = "QuickGuess Email Verification",
                Body = body,
                IsBodyHtml = true
            };

            await client.SendMailAsync(message);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request, [FromServices] ApplicationDbContext db)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.Provider == "local");
            if (user == null || user.PasswordHash == null || user.PasswordSalt == null)
                return Unauthorized("Invalid credentials");

            if (!PasswordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
                return Unauthorized("Invalid credentials");

            if (!user.EmailVerified)
                return Unauthorized("Please verify your email first.");

            var token = _jwt.GenerateToken(user);
            return Ok(new AuthResponse
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Role = user.AccountType
            });
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request, [FromServices] ApplicationDbContext db)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
            }
            catch
            {
                return Unauthorized("Invalid Google token");
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == payload.Email && u.Provider == "google");

            if (user == null)
            {
                user = new User
                {
                    Email = payload.Email,
                    Username = payload.Name ?? payload.Email.Split('@')[0],
                    EmailVerified = true,
                    Provider = "google",
                    GoogleId = payload.Subject,
                    AccountType = "user" // ← ważne!
                };
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }

            var token = _jwt.GenerateToken(user);
            return Ok(new AuthResponse
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Role = user.AccountType
            });
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token, [FromServices] ApplicationDbContext db)
        {
            var record = await db.EmailVerificationTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (record == null || record.Used || record.ExpiresAt < DateTime.UtcNow)
                return BadRequest("Invalid or expired token");

            var user = await db.Users.FindAsync(record.UserId);
            if (user == null) return NotFound();

            user.EmailVerified = true;
            record.Used = true;

            await db.SaveChangesAsync();
            return Ok("Email verified.");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, [FromServices] ApplicationDbContext db)
        {
            var token = await db.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == request.Token);
            if (token == null || token.Used || token.ExpiresAt < DateTime.UtcNow)
                return BadRequest("Invalid or expired token");

            var user = await db.Users.FindAsync(token.UserId);
            if (user == null) return NotFound();

            PasswordHasher.CreateHash(request.NewPassword, out string hash, out string salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            token.Used = true;

            await db.SaveChangesAsync();
            return Ok("Password updated successfully.");
        }

        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset(RequestPasswordReset request, [FromServices] ApplicationDbContext db)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.Provider == "local");
            if (user == null)
                return Ok("If that email exists, a reset link has been sent."); // nie ujawniamy

            var token = TokenGenerator.GenerateToken(32);

            db.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });

            await db.SaveChangesAsync();
            await SendResetEmail(user.Email, token);
            return Ok("Reset link sent.");
        }

        private async Task SendResetEmail(string email, string token)
        {
            var resetUrl = $"https://localhost:7236/reset-password?token={Uri.EscapeDataString(token)}";
            var body = $"<p>Click the link to reset your password:</p><p><a href=\"{resetUrl}\">Reset Password</a></p>";

            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential("your.email@gmail.com", "your_app_password")
            };

            var message = new MailMessage("your.email@gmail.com", email)
            {
                Subject = "QuickGuess - Password Reset",
                Body = body,
                IsBodyHtml = true
            };

            await client.SendMailAsync(message);
        }



    }
}
