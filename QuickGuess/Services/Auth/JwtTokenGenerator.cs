using System.IdentityModel.Tokens.Jwt;
//using Microsoft.IdentityModel.Tokens;
using QuickGuess.Models;
using QuickGuess.Models.Settings;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;


namespace QuickGuess.Services.Auth
{
    public class JwtTokenGenerator
    {
        private readonly JwtSettings _jwtSettings;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            _jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()!;
        }

        public string GenerateToken(User user)
        {
            //var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            //var key = new SymmetricSecurityKey(Convert.FromBase64String(_jwtSettings.Secret));
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));

            Console.WriteLine(">>> JWT secret used in token generator: " + _jwtSettings.Secret);



            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), 
                new Claim(ClaimTypes.Role, user.AccountType),

            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.TokenLifetimeMinutes),
                signingCredentials: creds
            );

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(new JwtSecurityTokenHandler().WriteToken(token));
            Console.WriteLine(">>> Decoded JWT header:");
            foreach (var item in jwt.Header)
            {
                Console.WriteLine($" - {item.Key}: {item.Value}");
            }

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
