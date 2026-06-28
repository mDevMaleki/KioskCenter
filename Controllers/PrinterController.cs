using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KioskCenter.Authorization;
using KioskCenter.Data;
using KioskCenter.Interfaces;
using KioskCenter.Models;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrinterController : ControllerBase
    {
        private readonly CoffeeShopContext _context;
        private readonly ILogger<PrinterController> _logger;
        private readonly IReceiptPrinter _receiptPrinter;

        public PrinterController(
            CoffeeShopContext context,
            ILogger<PrinterController> logger,
            IReceiptPrinter receiptPrinter)
        {
            _context = context;
            _logger = logger;
            _receiptPrinter = receiptPrinter;
        }

        // دریافت لیست تمام پرینترها
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var printers = await _context.PrinterSettings
                .OrderBy(p => p.Priority)
                .ToListAsync();
            return Ok(printers);
        }

        // دریافت پرینترهای فعال
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var printers = await _context.PrinterSettings
                .Where(p => p.IsActive)
                .OrderBy(p => p.Priority)
                .ToListAsync();
            return Ok(printers);
        }

        // دریافت پرینتر بر اساس نوع
        [HttpGet("type/{type}")]
        public async Task<IActionResult> GetByType(string type)
        {
            var printers = await _context.PrinterSettings
                .Where(p => p.PrinterType == type && p.IsActive)
                .ToListAsync();
            return Ok(printers);
        }

        // دریافت یک پرینتر
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var printer = await _context.PrinterSettings.FindAsync(id);
            if (printer == null)
                return NotFound(new { success = false, message = "پرینتر یافت نشد" });
            return Ok(printer);
        }

        // ایجاد پرینتر جدید
        [Authorize, RequirePermission("printers")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PrinterSetting printer)
        {
            try
            {
                printer.CreatedAt = DateTime.Now;
                _context.PrinterSettings.Add(printer);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "پرینتر با موفقیت اضافه شد", data = printer });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ویرایش پرینتر
        [Authorize, RequirePermission("printers")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PrinterSetting printer)
        {
            if (id != printer.Id)
                return BadRequest(new { success = false, message = "شناسه نامعتبر" });

            var existing = await _context.PrinterSettings.FindAsync(id);
            if (existing == null)
                return NotFound(new { success = false, message = "پرینتر یافت نشد" });

            existing.Name = printer.Name;
            existing.PrinterName = printer.PrinterName;
            existing.PrinterType = printer.PrinterType;
            existing.IsActive = printer.IsActive;
            existing.Priority = printer.Priority;
            existing.Categories = printer.Categories;
            existing.ProductIds = printer.ProductIds;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "پرینتر با موفقیت ویرایش شد", data = existing });
        }

        // حذف پرینتر
        [Authorize, RequirePermission("printers")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var printer = await _context.PrinterSettings.FindAsync(id);
            if (printer == null)
                return NotFound(new { success = false, message = "پرینتر یافت نشد" });

            _context.PrinterSettings.Remove(printer);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "پرینتر با موفقیت حذف شد" });
        }

        // تست پرینتر
        [Authorize, RequirePermission("printers")]
        [HttpPost("test/{id}")]
        public async Task<IActionResult> TestPrinter(int id)
        {
            var printer = await _context.PrinterSettings.FindAsync(id);
            if (printer == null)
                return NotFound(new { success = false, message = "پرینتر یافت نشد" });

            var testResult = _receiptPrinter.TestPrint(printer.PrinterName);
            return Ok(new { success = testResult, message = testResult ? "پرینتر با موفقیت تست شد" : "خطا در تست پرینتر" });
        }

        // دریافت لیست پرینترهای نصب شده در ویندوز
        [Authorize, RequirePermission("printers")]
        [HttpGet("installed")]
        public IActionResult GetInstalledPrinters()
        {
            var printers = System.Drawing.Printing.PrinterSettings.InstalledPrinters
                .Cast<string>()
                .ToList();
            return Ok(printers);
        }
    }
}