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
    [Authorize, RequirePermission("inventory")]
    public class InventoryController : ControllerBase
    {
        private readonly CoffeeShopContext _context;

        public InventoryController(CoffeeShopContext context)
        {
            _context = context;
        }

        // GET: api/inventory/products - وضعیت موجودی محصولات
        [HttpGet("products")]
        public async Task<IActionResult> GetProductsStock([FromQuery] int? categoryId, [FromQuery] string? search, [FromQuery] bool lowStockOnly = false)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search));

            var products = await query
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    p.ImageUrl,
                    p.StockQuantity,
                    p.MinStockLevel,
                    p.Unit
                })
                .ToListAsync();

            if (lowStockOnly)
                products = products.Where(p => p.StockQuantity <= p.MinStockLevel).ToList();

            return Ok(products);
        }

        // GET: api/inventory/transactions - تاریخچه ورود و خروج کالا
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] int? productId,
            [FromQuery] InventoryTransactionType? type,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _context.InventoryTransactions.Include(t => t.Product).AsQueryable();

            if (productId.HasValue)
                query = query.Where(t => t.ProductId == productId.Value);

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
                    t.ProductId,
                    ProductName = t.Product != null ? t.Product.Name : null,
                    t.Type,
                    t.Quantity,
                    t.StockAfter,
                    t.UnitPrice,
                    t.Note,
                    t.CreatedAt
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        // POST: api/inventory/stock-in - ثبت ورود کالا
        [HttpPost("stock-in")]
        public async Task<IActionResult> StockIn([FromBody] StockChangeRequest request)
        {
            if (request.Quantity <= 0)
                return BadRequest(new { success = false, message = "مقدار باید بیشتر از صفر باشد" });

            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
                return NotFound(new { success = false, message = "محصول یافت نشد" });

            product.StockQuantity += request.Quantity;

            var tx = new InventoryTransaction
            {
                ProductId = product.Id,
                Type = InventoryTransactionType.In,
                Quantity = request.Quantity,
                StockAfter = product.StockQuantity,
                UnitPrice = request.UnitPrice,
                Note = request.Note,
                CreatedAt = DateTime.Now
            };

            _context.InventoryTransactions.Add(tx);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "ورود کالا با موفقیت ثبت شد", stock = product.StockQuantity });
        }

        // POST: api/inventory/stock-out - ثبت خروج کالا
        [HttpPost("stock-out")]
        public async Task<IActionResult> StockOut([FromBody] StockChangeRequest request)
        {
            if (request.Quantity <= 0)
                return BadRequest(new { success = false, message = "مقدار باید بیشتر از صفر باشد" });

            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
                return NotFound(new { success = false, message = "محصول یافت نشد" });

            if (product.StockQuantity < request.Quantity)
                return BadRequest(new { success = false, message = "موجودی کافی نیست" });

            product.StockQuantity -= request.Quantity;

            var tx = new InventoryTransaction
            {
                ProductId = product.Id,
                Type = InventoryTransactionType.Out,
                Quantity = request.Quantity,
                StockAfter = product.StockQuantity,
                UnitPrice = request.UnitPrice,
                Note = request.Note,
                CreatedAt = DateTime.Now
            };

            _context.InventoryTransactions.Add(tx);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "خروج کالا با موفقیت ثبت شد", stock = product.StockQuantity });
        }

        // POST: api/inventory/adjust - اصلاح موجودی
        [HttpPost("adjust")]
        public async Task<IActionResult> Adjust([FromBody] AdjustStockRequest request)
        {
            if (request.NewQuantity < 0)
                return BadRequest(new { success = false, message = "موجودی نمی‌تواند منفی باشد" });

            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
                return NotFound(new { success = false, message = "محصول یافت نشد" });

            var diff = request.NewQuantity - product.StockQuantity;
            if (diff == 0)
                return Ok(new { success = true, message = "تغییری اعمال نشد", stock = product.StockQuantity });

            product.StockQuantity = request.NewQuantity;

            var tx = new InventoryTransaction
            {
                ProductId = product.Id,
                Type = InventoryTransactionType.Adjustment,
                Quantity = diff,
                StockAfter = product.StockQuantity,
                Note = request.Note,
                CreatedAt = DateTime.Now
            };

            _context.InventoryTransactions.Add(tx);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "موجودی با موفقیت اصلاح شد", stock = product.StockQuantity });
        }

        // PUT: api/inventory/settings/{productId} - تنظیم حد آستانه و واحد شمارش
        [HttpPut("settings/{productId}")]
        public async Task<IActionResult> UpdateSettings(int productId, [FromBody] InventorySettingsRequest request)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound(new { success = false, message = "محصول یافت نشد" });

            if (request.MinStockLevel.HasValue)
                product.MinStockLevel = request.MinStockLevel.Value;

            if (!string.IsNullOrWhiteSpace(request.Unit))
                product.Unit = request.Unit;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "تنظیمات با موفقیت ذخیره شد" });
        }
    }

    public class StockChangeRequest
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Note { get; set; }
    }

    public class AdjustStockRequest
    {
        public int ProductId { get; set; }
        public decimal NewQuantity { get; set; }
        public string? Note { get; set; }
    }

    public class InventorySettingsRequest
    {
        public decimal? MinStockLevel { get; set; }
        public string? Unit { get; set; }
    }
}
