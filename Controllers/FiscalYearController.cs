using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KioskCenter.Authorization;
using KioskCenter.Data;
using KioskCenter.Models;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize, RequirePermission("accounting")]
    public class FiscalYearController : ControllerBase
    {
        private readonly CoffeeShopContext _context;

        public FiscalYearController(CoffeeShopContext context)
        {
            _context = context;
        }

        // GET: api/fiscalyear
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _context.FiscalYears.OrderByDescending(f => f.StartDate).ToListAsync();
            return Ok(items);
        }

        // GET: api/fiscalyear/current
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent()
        {
            var now = DateTime.Now;
            var current = await _context.FiscalYears
                .Where(f => now >= f.StartDate && now <= f.EndDate)
                .FirstOrDefaultAsync();

            if (current == null)
                return Ok(new { exists = false });

            return Ok(new { exists = true, fiscalYear = current });
        }

        // POST: api/fiscalyear
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FiscalYearDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.StartDate >= dto.EndDate)
                return BadRequest(new { success = false, message = "نام و بازه زمانی معتبر الزامی است" });

            var overlap = await _context.FiscalYears.AnyAsync(f =>
                dto.StartDate <= f.EndDate && dto.EndDate >= f.StartDate);

            if (overlap)
                return BadRequest(new { success = false, message = "این بازه زمانی با یک سال مالی دیگر تداخل دارد" });

            var fiscalYear = new FiscalYear
            {
                Name = dto.Name,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsClosed = false,
                CreatedAt = DateTime.Now
            };

            _context.FiscalYears.Add(fiscalYear);
            await _context.SaveChangesAsync();

            return Ok(fiscalYear);
        }

        // POST: api/fiscalyear/{id}/close - بستن سال مالی (قطعی کردن اسناد)
        [HttpPost("{id}/close")]
        public async Task<IActionResult> Close(int id)
        {
            var fiscalYear = await _context.FiscalYears.FindAsync(id);
            if (fiscalYear == null) return NotFound();
            if (fiscalYear.IsClosed)
                return BadRequest(new { success = false, message = "این سال مالی قبلاً بسته شده است" });

            fiscalYear.IsClosed = true;
            fiscalYear.ClosedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "سال مالی با موفقیت بسته شد و دیگر امکان ثبت سند جدید در این بازه وجود ندارد" });
        }

        // POST: api/fiscalyear/{id}/reopen - بازگشایی سال مالی (احتیاطی)
        [HttpPost("{id}/reopen")]
        public async Task<IActionResult> Reopen(int id)
        {
            var fiscalYear = await _context.FiscalYears.FindAsync(id);
            if (fiscalYear == null) return NotFound();

            fiscalYear.IsClosed = false;
            fiscalYear.ClosedAt = null;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "سال مالی بازگشایی شد" });
        }

        // DELETE: api/fiscalyear/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var fiscalYear = await _context.FiscalYears.FindAsync(id);
            if (fiscalYear == null) return NotFound();
            if (fiscalYear.IsClosed)
                return BadRequest(new { success = false, message = "سال مالی بسته‌شده قابل حذف نیست" });

            _context.FiscalYears.Remove(fiscalYear);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }

    public class FiscalYearDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
