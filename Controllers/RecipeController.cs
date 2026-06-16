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
    public class RecipeController : ControllerBase
    {
        private readonly CoffeeShopContext _context;

        public RecipeController(CoffeeShopContext context)
        {
            _context = context;
        }

        // GET: api/recipe/{productId} - فرمول یک محصول
        [HttpGet("{productId}")]
        public async Task<IActionResult> GetByProduct(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound(new { success = false, message = "محصول یافت نشد" });

            var items = await _context.ProductRecipeItems
                .Where(r => r.ProductId == productId)
                .Include(r => r.RawMaterial).ThenInclude(m => m!.Unit)
                .Select(r => new
                {
                    r.Id,
                    r.RawMaterialId,
                    RawMaterialName = r.RawMaterial != null ? r.RawMaterial.Name : null,
                    Unit = r.RawMaterial != null && r.RawMaterial.Unit != null ? r.RawMaterial.Unit.Name : null,
                    r.Quantity
                })
                .ToListAsync();

            return Ok(new { productId, productName = product.Name, items });
        }

        // PUT: api/recipe/{productId} - ذخیره فرمول یک محصول
        [HttpPut("{productId}")]
        public async Task<IActionResult> SetRecipe(int productId, [FromBody] List<RecipeItemDto> items)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound(new { success = false, message = "محصول یافت نشد" });

            if (items.Any(i => i.Quantity <= 0))
                return BadRequest(new { success = false, message = "مقدار مصرفی باید بیشتر از صفر باشد" });

            var materialIds = items.Select(i => i.RawMaterialId).ToList();
            var existingMaterialCount = await _context.RawMaterials.CountAsync(m => materialIds.Contains(m.Id));
            if (existingMaterialCount != materialIds.Distinct().Count())
                return BadRequest(new { success = false, message = "یکی از مواد اولیه انتخاب‌شده یافت نشد" });

            var existing = _context.ProductRecipeItems.Where(r => r.ProductId == productId);
            _context.ProductRecipeItems.RemoveRange(existing);

            foreach (var item in items)
            {
                _context.ProductRecipeItems.Add(new ProductRecipeItem
                {
                    ProductId = productId,
                    RawMaterialId = item.RawMaterialId,
                    Quantity = item.Quantity
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "فرمول محصول با موفقیت ذخیره شد" });
        }
    }

    public class RecipeItemDto
    {
        public int RawMaterialId { get; set; }
        public decimal Quantity { get; set; }
    }
}
