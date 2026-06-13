using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KioskCenter.Data;
using KioskCenter.Models;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentMethodController : ControllerBase
    {
        private readonly CoffeeShopContext _context;
        private readonly ILogger<PaymentMethodController> _logger;

        public PaymentMethodController(CoffeeShopContext context, ILogger<PaymentMethodController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // دریافت لیست روش‌های پرداخت فعال
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var methods = await _context.PaymentMethods
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ToListAsync();
            return Ok(methods);
        }

        // دریافت همه روش‌های پرداخت
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var methods = await _context.PaymentMethods
                .OrderBy(p => p.SortOrder)
                .ToListAsync();
            return Ok(methods);
        }

        // دریافت یک روش پرداخت
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var method = await _context.PaymentMethods.FindAsync(id);
            if (method == null)
                return NotFound(new { success = false, message = "روش پرداخت یافت نشد" });
            return Ok(method);
        }

        // ایجاد روش پرداخت جدید
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PaymentMethod method)
        {
            try
            {
                method.CreatedAt = DateTime.Now;
                _context.PaymentMethods.Add(method);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "روش پرداخت با موفقیت اضافه شد", data = method });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ویرایش روش پرداخت
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PaymentMethod method)
        {
            if (id != method.Id)
                return BadRequest(new { success = false, message = "شناسه نامعتبر" });

            var existing = await _context.PaymentMethods.FindAsync(id);
            if (existing == null)
                return NotFound(new { success = false, message = "روش پرداخت یافت نشد" });

            existing.Name = method.Name;
            existing.Title = method.Title;
            existing.Icon = method.Icon;
            existing.Description = method.Description;
            existing.IsActive = method.IsActive;
            existing.SortOrder = method.SortOrder;
            existing.RequiresPosDevice = method.RequiresPosDevice;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "روش پرداخت با موفقیت ویرایش شد", data = existing });
        }

        // حذف روش پرداخت
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var method = await _context.PaymentMethods.FindAsync(id);
            if (method == null)
                return NotFound(new { success = false, message = "روش پرداخت یافت نشد" });

            _context.PaymentMethods.Remove(method);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "روش پرداخت با موفقیت حذف شد" });
        }

        // مقداردهی اولیه روش‌های پرداخت پیش‌فرض
        [HttpPost("seed")]
        public async Task<IActionResult> Seed()
        {
            var defaultMethods = new List<PaymentMethod>
            {
                new PaymentMethod { Name = "Cash", Title = "پرداخت نقدی", Icon = "💵", Description = "پرداخت با پول نقد", IsActive = true, SortOrder = 1, RequiresPosDevice = false },
                new PaymentMethod { Name = "Pos", Title = "کارت خوان (POS)", Icon = "💳", Description = "پرداخت با کارت بانکی", IsActive = true, SortOrder = 2, RequiresPosDevice = true },
                new PaymentMethod { Name = "Online", Title = "پرداخت آنلاین", Icon = "📱", Description = "پرداخت از طریق درگاه اینترنتی", IsActive = true, SortOrder = 3, RequiresPosDevice = false }
            };

            foreach (var method in defaultMethods)
            {
                if (!_context.PaymentMethods.Any(m => m.Name == method.Name))
                {
                    method.CreatedAt = DateTime.Now;
                    _context.PaymentMethods.Add(method);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "روش‌های پرداخت پیش‌فرض اضافه شدند" });
        }
    }
}