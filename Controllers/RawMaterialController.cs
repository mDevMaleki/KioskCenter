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
    public class RawMaterialController : ControllerBase
    {
        private readonly CoffeeShopContext _context;

        public RawMaterialController(CoffeeShopContext context)
        {
            _context = context;
        }

        // GET: api/rawmaterial - لیست مواد اولیه
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] bool lowStockOnly = false)
        {
            var query = _context.RawMaterials.Include(r => r.Unit).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(r => r.Name.Contains(search));

            var items = await query
                .OrderBy(r => r.Name)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.UnitId,
                    UnitName = r.Unit != null ? r.Unit.Name : null,
                    r.StockQuantity,
                    r.MinStockLevel,
                    r.CreatedAt
                })
                .ToListAsync();

            if (lowStockOnly)
                items = items.Where(r => r.StockQuantity <= r.MinStockLevel).ToList();

            return Ok(items);
        }

        // POST: api/rawmaterial - ایجاد ماده اولیه جدید
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RawMaterialDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { success = false, message = "نام ماده اولیه الزامی است" });

            var unit = await _context.UnitsOfMeasure.FindAsync(dto.UnitId);
            if (unit == null)
                return BadRequest(new { success = false, message = "واحد انتخاب‌شده معتبر نیست" });

            if (unit.BaseUnitId != null)
                return BadRequest(new { success = false, message = "واحد ذخیره موجودی باید یک واحد پایه باشد" });

            var material = new RawMaterial
            {
                Name = dto.Name,
                UnitId = dto.UnitId,
                MinStockLevel = dto.MinStockLevel,
                CreatedAt = DateTime.Now
            };

            _context.RawMaterials.Add(material);
            await _context.SaveChangesAsync();

            return Ok(material);
        }

        // PUT: api/rawmaterial/{id} - ویرایش ماده اولیه
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RawMaterialDto dto)
        {
            var material = await _context.RawMaterials.FindAsync(id);
            if (material == null)
                return NotFound(new { success = false, message = "ماده اولیه یافت نشد" });

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { success = false, message = "نام ماده اولیه الزامی است" });

            if (dto.UnitId != material.UnitId)
            {
                var unit = await _context.UnitsOfMeasure.FindAsync(dto.UnitId);
                if (unit == null)
                    return BadRequest(new { success = false, message = "واحد انتخاب‌شده معتبر نیست" });

                if (unit.BaseUnitId != null)
                    return BadRequest(new { success = false, message = "واحد ذخیره موجودی باید یک واحد پایه باشد" });

                material.UnitId = dto.UnitId;
            }

            material.Name = dto.Name;
            material.MinStockLevel = dto.MinStockLevel;

            await _context.SaveChangesAsync();

            return Ok(material);
        }

        // DELETE: api/rawmaterial/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var material = await _context.RawMaterials.FindAsync(id);
            if (material == null)
                return NotFound(new { success = false, message = "ماده اولیه یافت نشد" });

            var usedInRecipe = await _context.ProductRecipeItems.AnyAsync(r => r.RawMaterialId == id);
            if (usedInRecipe)
                return BadRequest(new { success = false, message = "این ماده اولیه در فرمول یک یا چند محصول استفاده شده است و قابل حذف نیست" });

            _context.RawMaterials.Remove(material);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "ماده اولیه با موفقیت حذف شد" });
        }

        // GET: api/rawmaterial/transactions - تاریخچه خرید و فروش مواد اولیه
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] int? rawMaterialId,
            [FromQuery] RawMaterialTransactionType? type,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _context.RawMaterialTransactions.Include(t => t.RawMaterial).ThenInclude(r => r!.Unit).AsQueryable();

            if (rawMaterialId.HasValue)
                query = query.Where(t => t.RawMaterialId == rawMaterialId.Value);

            if (type.HasValue)
                query = query.Where(t => t.Type == type.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    t.Id,
                    t.RawMaterialId,
                    RawMaterialName = t.RawMaterial != null ? t.RawMaterial.Name : null,
                    Unit = t.RawMaterial != null && t.RawMaterial.Unit != null ? t.RawMaterial.Unit.Name : null,
                    t.Type,
                    t.Quantity,
                    t.StockAfter,
                    t.UnitPrice,
                    t.TotalPrice,
                    t.PartyName,
                    t.Note,
                    t.CreatedAt
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        // POST: api/rawmaterial/consume - ثبت خروج/مصرف دستی ماده اولیه
        [HttpPost("consume")]
        public async Task<IActionResult> Consume([FromBody] RawMaterialTradeRequest request)
        {
            if (request.Quantity <= 0)
                return BadRequest(new { success = false, message = "مقدار باید بیشتر از صفر باشد" });

            var material = await _context.RawMaterials.FindAsync(request.RawMaterialId);
            if (material == null)
                return NotFound(new { success = false, message = "ماده اولیه یافت نشد" });

            if (material.StockQuantity < request.Quantity)
                return BadRequest(new { success = false, message = "موجودی کافی نیست" });

            material.StockQuantity -= request.Quantity;

            var totalPrice = request.TotalPrice ?? (request.UnitPrice.HasValue ? request.UnitPrice.Value * request.Quantity : (decimal?)null);

            var tx = new RawMaterialTransaction
            {
                RawMaterialId = material.Id,
                Type = RawMaterialTransactionType.Out,
                Quantity = request.Quantity,
                StockAfter = material.StockQuantity,
                UnitPrice = request.UnitPrice,
                TotalPrice = totalPrice,
                PartyName = request.PartyName,
                Note = request.Note,
                CreatedAt = DateTime.Now
            };

            _context.RawMaterialTransactions.Add(tx);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "خروج با موفقیت ثبت شد", stock = material.StockQuantity });
        }

        // POST: api/rawmaterial/adjust - اصلاح موجودی
        [HttpPost("adjust")]
        public async Task<IActionResult> Adjust([FromBody] AdjustRawMaterialRequest request)
        {
            if (request.NewQuantity < 0)
                return BadRequest(new { success = false, message = "موجودی نمی‌تواند منفی باشد" });

            var material = await _context.RawMaterials.FindAsync(request.RawMaterialId);
            if (material == null)
                return NotFound(new { success = false, message = "ماده اولیه یافت نشد" });

            var diff = request.NewQuantity - material.StockQuantity;
            if (diff == 0)
                return Ok(new { success = true, message = "تغییری اعمال نشد", stock = material.StockQuantity });

            material.StockQuantity = request.NewQuantity;

            var tx = new RawMaterialTransaction
            {
                RawMaterialId = material.Id,
                Type = RawMaterialTransactionType.Adjustment,
                Quantity = diff,
                StockAfter = material.StockQuantity,
                Note = request.Note,
                CreatedAt = DateTime.Now
            };

            _context.RawMaterialTransactions.Add(tx);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "موجودی با موفقیت اصلاح شد", stock = material.StockQuantity });
        }
    }

    public class RawMaterialDto
    {
        public string Name { get; set; } = string.Empty;
        public int UnitId { get; set; }
        public decimal MinStockLevel { get; set; }
    }

    public class RawMaterialTradeRequest
    {
        public int RawMaterialId { get; set; }
        public decimal Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? PartyName { get; set; }
        public string? Note { get; set; }
    }

    public class AdjustRawMaterialRequest
    {
        public int RawMaterialId { get; set; }
        public decimal NewQuantity { get; set; }
        public string? Note { get; set; }
    }
}
