using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickGuess.Data;
using QuickGuess.DTOs.Auth;
using QuickGuess.Models;
using QuickGuess.Services.Auth;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;

namespace QuickGuess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly JwtTokenGenerator _jwt;
        private readonly IWebHostEnvironment _env;
        // Branding / nadawca
        private readonly string _brandName = "QuickGuess";
        private readonly string _supportEmail = "quickguess.mail@gmail.com";

        // Konfiguracja aplikacji / linków
        private readonly string _appBaseUrl;
        private readonly string _appBaseUrlFrontend;

        // SMTP
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPassword;

        private readonly string _googleClientId;

        public AuthController(IConfiguration config, IWebHostEnvironment env)
        {
            _jwt = new JwtTokenGenerator(config);

            // Produkcyjny adres podstawowy – nie używamy localhost w mailach
            _appBaseUrl = config["App:BaseUrl"] ?? "https://localhost:7236";
            _appBaseUrlFrontend = config["App:FrontendBaseUrl"] ?? "https://localhost:7003";

            _smtpHost = config["Smtp:Host"] ?? "smtp.gmail.com";
            _smtpPort = int.TryParse(config["Smtp:Port"], out var p) ? p : 587;
            _smtpUser = config["Smtp:User"] ?? _supportEmail;
            _smtpPassword = config["Smtp:Password"] ?? ""; // ustaw przez Secret/ENV

            _googleClientId = config["GoogleAuth:ClientId"] ?? throw new InvalidOperationException("GoogleAuth:ClientId not set");
            _env = env;
        }

        // ======================= AUTH =======================

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
                Provider = "local",
                AccountType = "user",
                PublicId = Guid.NewGuid()
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            // token weryfikacyjny
            var token = TokenGenerator.GenerateToken(32);
            db.EmailVerificationTokens.Add(new EmailVerificationToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            });
            await db.SaveChangesAsync();

            await SendVerificationEmail(user.Email, token);

            return Ok("Registration successful. Check your email to verify your account.");
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

            if (user.PublicId is null || user.PublicId == Guid.Empty)
            {
                user.PublicId = Guid.NewGuid();
                await db.SaveChangesAsync();
            }

            var token = _jwt.GenerateToken(user);
            return Ok(new AuthResponse
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Role = user.AccountType,
                PublicId = user.PublicId!.Value
            });
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request, [FromServices] ApplicationDbContext db)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(
                    request.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { _googleClientId }
                    });
            }
            catch
            {
                return Unauthorized("Invalid Google token");
            }

            var email = (payload.Email ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(email))
                return Unauthorized("Google token has no email");

            // ⬇️ KLUCZ: szukamy po e-mailu, niezależnie od provider'a
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
            {
                // brak konta – tworzymy nowe „google”
                user = new User
                {
                    Email = payload.Email,
                    Username = payload.Name ?? payload.Email.Split('@')[0],
                    EmailVerified = true, 
                    Provider = "google",
                    GoogleId = payload.Subject,
                    AccountType = "user",
                    PublicId = Guid.NewGuid()
                };
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }
            else
            {
                // konto już istnieje (np. 'local') – podpinamy Google do TEGO konta
                if (string.IsNullOrWhiteSpace(user.GoogleId))
                    user.GoogleId = payload.Subject;

                // jeśli konto lokalne nie było zweryfikowane – uznaj weryfikację Google
                if (!user.EmailVerified)
                    user.EmailVerified = true;    
                

                // nie musimy zmieniać 'Provider' – konto może pozostać 'local'
                if (user.PublicId is null || user.PublicId == Guid.Empty)
                    user.PublicId = Guid.NewGuid();

                await db.SaveChangesAsync();
            }

            var token = _jwt.GenerateToken(user);
            return Ok(new AuthResponse
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Role = user.AccountType,
                PublicId = user.PublicId!.Value
            });
        }


        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token, [FromServices] ApplicationDbContext db)
        {
            var record = await db.EmailVerificationTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (record == null || record.Used || record.ExpiresAt < DateTime.UtcNow)
                return Redirect($"{_appBaseUrlFrontend.TrimEnd('/')}/verified?status=error");

            var user = await db.Users.FindAsync(record.UserId);
            if (user == null)
                return Redirect($"{_appBaseUrlFrontend.TrimEnd('/')}/verified?status=error");

            user.EmailVerified = true;
            record.Used = true;
            await db.SaveChangesAsync();

            return Redirect($"{_appBaseUrlFrontend.TrimEnd('/')}/verified?status=ok");
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

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, [FromServices] ApplicationDbContext db)
        {
            // 1) spróbuj ID jako int z kilku znanych claimów
            int? userId = null;
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                       ?? User.FindFirst("uid")
                       ?? User.FindFirst("id")
                       ?? User.FindFirst("sub");

            if (idClaim != null && int.TryParse(idClaim.Value, out var parsed))
                userId = parsed;

            QuickGuess.Models.User? user = null;

            if (userId.HasValue)
            {
                user = await db.Users.FindAsync(userId.Value);
            }
            else
            {
                // 2) fallback: znajdź po e-mailu z tokena
                var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
                if (!string.IsNullOrWhiteSpace(email))
                    user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            }

            if (user == null)
                return BadRequest("Nie udało się zidentyfikować użytkownika z tokenu.");

            if (user.Provider != "local")
                return BadRequest("Hasło można zmienić tylko dla kont lokalnych.");

            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest("Brak wymaganych pól.");

            if (!PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash!, user.PasswordSalt!))
                return BadRequest("Obecne hasło jest nieprawidłowe.");

            bool ok = request.NewPassword.Length >= 8
                      && request.NewPassword.Any(char.IsUpper)
                      && request.NewPassword.Any(char.IsLower)
                      && request.NewPassword.Any(char.IsDigit)
                      && request.NewPassword.Any(c => !char.IsLetterOrDigit(c));
            if (!ok)
                return BadRequest("Nowe hasło nie spełnia wymagań bezpieczeństwa.");

            PasswordHasher.CreateHash(request.NewPassword, out string hash, out string salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;

            await db.SaveChangesAsync();
            return Ok("Password updated successfully.");
        }


        // ======================= MAILE =======================



        private async Task SendVerificationEmail(string email, string token)
        {
            var verifyUrl = $"{_appBaseUrl}/api/auth/verify-email?token={Uri.EscapeDataString(token)}";

            string subject = $"{_brandName} — Potwierdź adres e-mail";
            string preheader = "Kliknij, aby aktywować konto i zacząć grać w QuickGuess.";
            string title = "Witaj w QuickGuess!";
            string intro = "Dziękujemy za rejestrację. Potwierdź swój adres e-mail, aby utworzyć konto.";
            string button = "Potwierdź e-mail";

            await SendBrandedEmail(
                recipient: email,
                subject: subject,
                htmlBody: BuildBrandedHtml(preheader, title, intro, button, verifyUrl,
                    footerNote: "Link wygaśnie za 24 godziny. Jeśli to nie Ty zakładałeś konto — zignoruj tę wiadomość."),
                textBody: BuildPlainText(title, intro, button, verifyUrl)
            );
        }

        private async Task SendResetEmail(string email, string token)
        {
            // ← kierujemy na frontend
            var resetUrl = $"{_appBaseUrlFrontend}/reset-password?token={Uri.EscapeDataString(token)}";

            string subject = $"{_brandName} — Reset hasła";
            string preheader = "Zmień swoje hasło jednym kliknięciem.";
            string title = "Reset hasła";
            string intro = "Otrzymaliśmy prośbę o zresetowanie Twojego hasła. Jeśli to Ty — kliknij poniżej.";
            string button = "Ustaw nowe hasło";

            await SendBrandedEmail(
                recipient: email,
                subject: subject,
                htmlBody: BuildBrandedHtml(preheader, title, intro, button, resetUrl,
                    footerNote: "Ten link wygasa za 60 minut. Jeżeli nie prosiłeś o reset — możesz zignorować wiadomość."),
                textBody: BuildPlainText(title, intro, button, resetUrl)
            );
        }

        private async Task SendBrandedEmail(string recipient, string subject, string htmlBody, string textBody)
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_smtpUser, _smtpPassword)
            };

            var from = new MailAddress(_supportEmail, _brandName, Encoding.UTF8);
            var to = new MailAddress(recipient);

            using var message = new MailMessage(from, to)
            {
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
                HeadersEncoding = Encoding.UTF8,
                IsBodyHtml = true
            };

            // multipart/alternative: najpierw TXT, potem HTML
            var plainView = AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, "text/plain");
            var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, "text/html");

            var logoPath = Path.Combine(_env.WebRootPath, "media", "logonapisbialy.png");
            if (System.IO.File.Exists(logoPath))
            {
                var logo = new LinkedResource(logoPath, "image/png")
                {
                    ContentId = "app-logo",
                    TransferEncoding = System.Net.Mime.TransferEncoding.Base64
                };
                htmlView.LinkedResources.Add(logo);
            }

            message.AlternateViews.Add(plainView);
            message.AlternateViews.Add(htmlView);

            await client.SendMailAsync(message);
        }

        private string BuildBrandedHtml(string preheader, string title, string intro, string buttonText, string buttonUrl, string? footerNote = null)
        {
            return $@"
<!doctype html>
<html lang=""pl""><head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width,initial-scale=1"">
<title>{_brandName}</title>
<style>
body,table,td,a{{-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%}}
table,td{{mso-table-lspace:0pt;mso-table-rspace:0pt}}
img{{-ms-interpolation-mode:bicubic;border:0;outline:none;text-decoration:none}}
table{{border-collapse:collapse!important}}
:root{{color-scheme:dark light; supported-color-schemes:dark light;}}
body{{margin:0;padding:0;background:#0b0f1a;font-family:Segoe UI,Roboto,Arial,sans-serif}}
.wrapper{{width:100%;background:#0b0f1a;padding:24px 12px}}
.container{{max-width:600px;margin:0 auto;background:#111827;border-radius:16px;box-shadow:0 12px 40px rgba(0,0,0,.45);overflow:hidden}}
.brandbar{{height:6px;background:linear-gradient(90deg,#ffffff,#ffe400,#22c55e,#38bdf8,#ff2097,#ffffff)}}
.inner{{padding:32px 28px}}
.h1{{margin:0 0 8px;font-size:24px;line-height:1.25;color:#ffffff;font-weight:800;text-align:center}}
.lead{{margin:0 0 20px;font-size:15px;line-height:1.6;color:#cbd5e1}}
.card{{background:#0f172a;border:1px solid rgba(255,255,255,.08);border-radius:14px;padding:18px 16px;margin:0 0 22px;color:#e5e7eb}}
/* PRZYCISK: biały tekst na wszystkich stanach + !important */
.btn{{display:inline-block;padding:14px 22px;border-radius:9999px;font-weight:800;text-decoration:none;
     background:linear-gradient(180deg,#38bdf8,#0ea5e9); color:#ffffff!important; border:0; 
     box-shadow:0 10px 24px rgba(14,165,233,.35)}}
a.btn, a.btn:link, a.btn:visited, a.btn:hover, a.btn:active{{color:#ffffff!important; text-decoration:none!important}}
.btn:hover{{filter:brightness(1.05)}}
.note{{margin-top:22px;font-size:12px;color:#9ca3af}}
.footer{{text-align:center;padding:18px 22px 30px;font-size:12px;color:#94a3b8}}
.logo{{display:inline-block;font-weight:900;font-size:20px;color:#fff;text-decoration:none}}
@media (max-width:600px){{.inner{{padding:24px 18px}} .h1{{font-size:22px}}}}
</style>
</head>
<body>
<span style=""display:none!important;opacity:0;visibility:hidden;mso-hide:all;height:0;width:0;overflow:hidden"">{preheader}</span>

<div class=""wrapper"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
    <tr><td align=""center"">
      <table role=""presentation"" width=""100%"" class=""container"">
        <tr><td class=""brandbar""></td></tr>
        <tr><td class=""inner"">

          <div style=""text-align:center;margin:0 0 16px"">
  <a href=""{_appBaseUrlFrontend}"" target=""_blank"" style=""text-decoration:none;display:inline-block"">
    <img src=""cid:app-logo""
         alt=""{_brandName}""
         width=""120""
         style=""display:block;margin:0 auto;border:0;outline:none;text-decoration:none;max-width:160px;height:auto"" />
  </a>
</div>

          <div class=""card"">
            <h1 class=""h1"">{title}</h1>
            <p class=""lead"">{intro}</p>
            <p style=""text-align:center;margin:26px 0 10px"">
              <a class=""btn"" href=""{buttonUrl}"" target=""_blank"" style=""color:#ffffff!important"">{buttonText}</a>
            </p>
          </div>

          {(string.IsNullOrWhiteSpace(footerNote) ? "" : $"<div class=\"note\">{footerNote}</div>")}

          <div class=""footer"">
            Wysłano przez {_brandName}. W razie pytań napisz na <a href=""mailto:{_supportEmail}"" style=""color:#38bdf8;text-decoration:none"">{_supportEmail}</a>.<br/>
            © {DateTime.UtcNow.Year} {_brandName}. Wszelkie prawa zastrzeżone.
          </div>

        </td></tr>
      </table>
    </td></tr>
  </table>
</div>
</body></html>";
        }


        private string BuildPlainText(string title, string intro, string buttonText, string url)
        {
            return
$@"{title}

{intro}

{buttonText}:
{url}

—
{_brandName}";
        }
    }
}
