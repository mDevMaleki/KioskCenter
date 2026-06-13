using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KioskCenter.Models;
using System.ComponentModel.DataAnnotations;
using KioskCenter.Data;
using KioskCenter.Models;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly CoffeeShopContext _context;

        public ProductController(CoffeeShopContext context)
        {
            _context = context;
        }

        // GET: api/product - دریافت همه محصولات
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .ToListAsync();
            return Ok(products);
        }






        // GET: api/product/{id} - دریافت محصول با شناسه
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound(new { success = false, message = "محصول یافت نشد" });

            return Ok(product);
        }

        // GET: api/product/category/{categoryId} - دریافت محصولات یک دسته
        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(int categoryId)
        {
            var products = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Category)
                .ToListAsync();

            return Ok(products);
        }

        // POST: api/product - افزودن محصول جدید
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // بررسی وجود دسته بندی
            var category = await _context.Categories.FindAsync(request.CategoryId);
            if (category == null)
                return BadRequest(new { success = false, message = "دسته بندی نامعتبر است" });

            var product = new Product
            {
                Name = request.Name,
                Price = request.Price,
                SecondPrice = request.SecondPrice ,
                CategoryId = request.CategoryId,
                ImageUrl = request.ImageUrl,
                Description = request.Description,
                State = request.State

            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "محصول با موفقیت اضافه شد", product });
        }

        // PUT: api/product/{id} - ویرایش محصول
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { success = false, message = "محصول یافت نشد" });

            if (request.CategoryId.HasValue)
            {
                var category = await _context.Categories.FindAsync(request.CategoryId);
                if (category == null)
                    return BadRequest(new { success = false, message = "دسته بندی نامعتبر است" });
                product.CategoryId = request.CategoryId.Value;
            }

            if (!string.IsNullOrEmpty(request.Name))
                product.Name = request.Name;

            if (request.Price.HasValue)
                product.Price = request.Price.Value;

            if (request.SecondPrice.HasValue)
                product.SecondPrice = request.SecondPrice.Value;

            if (!string.IsNullOrEmpty(request.ImageUrl))
                product.ImageUrl = request.ImageUrl;

            if (!string.IsNullOrEmpty(request.Description))
                product.Description = request.Description;
            product.State = request.State;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "محصول با موفقیت ویرایش شد", product });
        }

        // DELETE: api/product/{id} - حذف محصول
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { success = false, message = "محصول یافت نشد" });

            // بررسی وجود سفارشات برای این محصول
            var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
            if (hasOrders)
                return BadRequest(new { success = false, message = "این محصول در سفارشات استفاده شده است و قابل حذف نیست" });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "محصول با موفقیت حذف شد" });
        }
        // اضافه کنید به ProductController.cs

        // GET: api/product/with-price-factor/{orderTypeId}
        [HttpGet("with-price-factor/{orderTypeId}")]
        public async Task<IActionResult> GetProductsWithPriceFactor(string orderTypeId, [FromQuery] int? categoryId = null)
        {
            try
            {
                var query = _context.Products.AsQueryable();

                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                var products = await query
                    .Include(p => p.Category)
                    .ToListAsync();

                // دریافت تنظیمات نوع سفارش
                var settingsEntity = await _context.OrderTypeSettings
                    .FirstOrDefaultAsync(s => s.OrderTypeId == orderTypeId);

                OrderTypeSettingDto? settings = null;
                if (settingsEntity != null && !string.IsNullOrEmpty(settingsEntity.SettingsJson))
                {
                    settings = System.Text.Json.JsonSerializer.Deserialize<OrderTypeSettingDto>(settingsEntity.SettingsJson);
                }

                var result = products.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.SecondPrice,
                    p.CategoryId,
                    CategoryName = p.Category?.Name,
                    p.ImageUrl,
                    p.Description,
                    p.State,
                    FinalPrice = CalculateFinalPrice(p.Price, settings, p.CategoryId)
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "خطا در دریافت محصولات" });
            }
        }

        private decimal CalculateFinalPrice(decimal originalPrice, OrderTypeSettingDto? settings, int categoryId)
        {
            if (settings == null || !settings.Active)
                return originalPrice;

            decimal factor = settings.PriceFactor;

            var categoryRule = settings.CategoryPriceRules
                .FirstOrDefault(r => r.CategoryId == categoryId.ToString());

            if (categoryRule != null)
            {
                factor = categoryRule.Factor;
            }

            return Math.Round(originalPrice * factor, 0);
        }
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("فایل معتبر نیست");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // اسم یکتا بسازیم
            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // مسیری که در دیتابیس ذخیره می‌شود
            var imageUrl = "/images/" + uniqueFileName;

            return Ok(new { imageUrl });
        }

    }

    public class CreateProductRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0, 99999999)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, 99999999)]
        public decimal SecondPrice { get; set; }

        public int State { get; set; } = 1;

        [Required]
        public int CategoryId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;

    }

    public class UpdateProductRequest
    {
        public string? Name { get; set; }
        public decimal? Price { get; set; }
   
        public decimal? SecondPrice { get; set; }
        public int? CategoryId { get; set; }
        public string? ImageUrl { get; set; }
        public int State { get; set; }
        public string? Description { get; set; }

    }
}