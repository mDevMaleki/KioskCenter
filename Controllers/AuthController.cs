using KioskCenter.Data;
using KioskCenter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly CoffeeShopContext _context;
        private readonly PasswordHasher _hasher;
        private readonly JwtService _jwtService;

        public AuthController(CoffeeShopContext context, PasswordHasher hasher, JwtService jwtService)
        {
            _context = context;
            _hasher = hasher;
            _jwtService = jwtService;
        }

        public class LoginDto
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);

            if (user == null || !user.IsActive || !_hasher.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { success = false, message = "نام کاربری یا رمز عبور اشتباه است" });

            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                success = true,
                token,
                user = new
                {
                    id = user.Id,
                    username = user.Username,
                    fullName = user.FullName,
                    isSuperAdmin = user.IsSuperAdmin,
                    permissions = user.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                }
            });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (idClaim == null || !int.TryParse(idClaim, out var userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return Unauthorized();

            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                fullName = user.FullName,
                isSuperAdmin = user.IsSuperAdmin,
                permissions = user.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            });
        }
    }
}
