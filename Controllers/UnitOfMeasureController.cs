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
    [Authorize, RequirePermission("purchase-sale")]
    public class UnitOfMeasureController : ControllerBase
    {
        private readonly CoffeeShopContext _context;

        public UnitOfMeasureController(CoffeeShopContext context)
        {
            _context = context;
        }

        // GET: api/unitofmeasure - لیست واحدها
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _context.UnitsOfMeasure
                .OrderBy(u => u.Name)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.BaseUnitId,
                    BaseUnitName = u.BaseUnit != null ? u.BaseUnit.Name : null,
                    u.ConversionFactor
                })
                .ToListAsync();

            return Ok(items);
        }

        // POST: api/unitofmeasure - ایجاد واحد جدید
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UnitOfMeasureDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { success = false, message = "نام واحد الزامی است" });

            if (dto.BaseUnitId.HasValue)
            {
                var baseUnit = await _context.UnitsOfMeasure.FindAsync(dto.BaseUnitId.Value);
                if (baseUnit == null)
                    return BadRequest(new { success = false, message = "واحد پایه انتخاب‌شده معتبر نیست" });

                if (baseUnit.BaseUnitId != null)
                    return BadRequest(new { success = false, message = "واحد پایه باید خودش یک واحد پایه باشد" });

                if (dto.ConversionFactor <= 0)
                    return BadRequest(new { success = false, message = "ضریب تبدیل باید بزرگ‌تر از صفر باشد" });
            }

            var unit = new UnitOfMeasure
            {
                Name = dto.Name,
                BaseUnitId = dto.BaseUnitId,
                ConversionFactor = dto.BaseUnitId.HasValue ? dto.ConversionFactor : 1
            };

            _context.UnitsOfMeasure.Add(unit);
            await _context.SaveChangesAsync();

            return Ok(unit);
        }

        // PUT: api/unitofmeasure/{id} - ویرایش واحد
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UnitOfMeasureDto dto)
        {
            var unit = await _context.UnitsOfMeasure.FindAsync(id);
            if (unit == null)
                return NotFound(new { success = false, message = "واحد یافت نشد" });

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { success = false, message = "نام واحد الزامی است" });

            if (dto.BaseUnitId.HasValue)
            {
                if (dto.BaseUnitId.Value == id)
                    return BadRequest(new { success = false, message = "یک واحد نمی‌تواند واحد پایه خودش باشد" });

                var baseUnit = await _context.UnitsOfMeasure.FindAsync(dto.BaseUnitId.Value);
                if (baseUnit == null)
                    return BadRequest(new { success = false, message = "واحد پایه انتخاب‌شده معتبر نیست" });

                if (baseUnit.BaseUnitId != null)
                    return BadRequest(new { success = false, message = "واحد پایه باید خودش یک واحد پایه باشد" });

                if (dto.ConversionFactor <= 0)
                    return BadRequest(new { success = false, message = "ضریب تبدیل باید بزرگ‌تر از صفر باشد" });

                // اگر این واحد قبلاً به‌عنوان واحد پایه برای واحدهای دیگر استفاده شده، نمی‌تواند خودش زیرمجموعه شود
                var isUsedAsBase = await _context.UnitsOfMeasure.AnyAsync(u => u.BaseUnitId == id);
                if (isUsedAsBase)
                    return BadRequest(new { success = false, message = "این واحد به‌عنوان واحد پایه برای واحدهای دیگر استفاده شده و نمی‌تواند زیرمجموعه شود" });

                // اگر این واحد به‌عنوان واحد ذخیره موجودی مواد اولیه استفاده شده
                var isUsedByMaterial = await _context.RawMaterials.AnyAsync(r => r.UnitId == id);
                if (isUsedByMaterial)
                    return BadRequest(new { success = false, message = "این واحد به‌عنوان واحد ذخیره موجودی یک ماده اولیه استفاده شده و نمی‌تواند زیرمجموعه شود" });
            }

            unit.Name = dto.Name;
            unit.BaseUnitId = dto.BaseUnitId;
            unit.ConversionFactor = dto.BaseUnitId.HasValue ? dto.ConversionFactor : 1;

            await _context.SaveChangesAsync();

            return Ok(unit);
        }

        // DELETE: api/unitofmeasure/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var unit = await _context.UnitsOfMeasure.FindAsync(id);
            if (unit == null)
                return NotFound(new { success = false, message = "واحد یافت نشد" });

            var isUsedAsBase = await _context.UnitsOfMeasure.AnyAsync(u => u.BaseUnitId == id);
            if (isUsedAsBase)
                return BadRequest(new { success = false, message = "این واحد به‌عنوان واحد پایه برای واحدهای دیگر استفاده شده و قابل حذف نیست" });

            var isUsedByMaterial = await _context.RawMaterials.AnyAsync(r => r.UnitId == id);
            if (isUsedByMaterial)
                return BadRequest(new { success = false, message = "این واحد توسط یک یا چند ماده اولیه استفاده شده و قابل حذف نیست" });

            var isUsedInPurchaseItems = await _context.PurchaseInvoiceItems.AnyAsync(i => i.UnitId == id);
            if (isUsedInPurchaseItems)
                return BadRequest(new { success = false, message = "این واحد در فاکتورهای خرید استفاده شده و قابل حذف نیست" });

            _context.UnitsOfMeasure.Remove(unit);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "واحد با موفقیت حذف شد" });
        }
    }

    public class UnitOfMeasureDto
    {
        public string Name { get; set; } = string.Empty;
        public int? BaseUnitId { get; set; }
        public decimal ConversionFactor { get; set; } = 1;
    }
}
