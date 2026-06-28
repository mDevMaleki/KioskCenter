using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KioskCenter.Models;
using Microsoft.IdentityModel.Tokens;

namespace KioskCenter.Services
{
    public class JwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string SecretKey => _config["Jwt:Key"] ?? "kiosk-center-default-dev-secret-key-change-me-1234567890";

        public string GenerateToken(AppUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("fullName", user.FullName),
                new Claim("isSuperAdmin", user.IsSuperAdmin ? "true" : "false")
            };

            if (!string.IsNullOrWhiteSpace(user.Permissions))
            {
                foreach (var perm in user.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    claims.Add(new Claim("perm", perm));
                }
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "KioskCenter",
                audience: "KioskCenter",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
