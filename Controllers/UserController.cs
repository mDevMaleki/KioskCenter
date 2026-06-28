using KioskCenter.Authorization;
using KioskCenter.Data;
using KioskCenter.Models;
using KioskCenter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [RequirePermission("users")]
    public class UserController : ControllerBase
    {
        private readonly CoffeeShopContext _context;
        private readonly PasswordHasher _hasher;

        public UserController(CoffeeShopContext context, PasswordHasher hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        public class UserDto
        {
            public string Username { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string? Password { get; set; }
            public bool IsSuperAdmin { get; set; }
            public bool IsActive { get; set; } = true;
            public List<string> Permissions { get; set; } = new();
        }

        [HttpGet("sections")]
        public IActionResult GetSections()
        {
            return Ok(AdminSections.All);
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.IsSuperAdmin,
                    u.IsActive,
                    u.CreatedAt,
                    Permissions = u.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { success = false, message = "نام کاربری و رمز عبور الزامی است" });

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return BadRequest(new { success = false, message = "این نام کاربری قبلاً ثبت شده است" });

            var user = new AppUser
            {
                Username = dto.Username,
                FullName = dto.FullName,
                PasswordHash = _hasher.Hash(dto.Password),
                IsSuperAdmin = dto.IsSuperAdmin,
                IsActive = dto.IsActive,
                Permissions = string.Join(",", dto.Permissions.Where(p => AdminSections.All.Contains(p)))
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, id = user.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { success = false, message = "کاربر یافت نشد" });

            if (dto.Username != user.Username && await _context.Users.AnyAsync(u => u.Username == dto.Username && u.Id != id))
                return BadRequest(new { success = false, message = "این نام کاربری قبلاً ثبت شده است" });

            user.Username = dto.Username;
            user.FullName = dto.FullName;
            user.IsSuperAdmin = dto.IsSuperAdmin;
            user.IsActive = dto.IsActive;
            user.Permissions = string.Join(",", dto.Permissions.Where(p => AdminSections.All.Contains(p)));

            if (!string.IsNullOrWhiteSpace(dto.Password))
                user.PasswordHash = _hasher.Hash(dto.Password);

            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { success = false, message = "کاربر یافت نشد" });

            if (await _context.Users.CountAsync(u => u.IsSuperAdmin) <= 1 && user.IsSuperAdmin)
                return BadRequest(new { success = false, message = "حداقل یک کاربر سوپر ادمین باید باقی بماند" });

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
}
