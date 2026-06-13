using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KioskCenter.Models;
using System.ComponentModel.DataAnnotations;
using KioskCenter.Data;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly CoffeeShopContext _context;

        public CategoryController(CoffeeShopContext context)
        {
            _context = context;
        }

        // GET: api/category - دریافت همه دسته بندی‌ها
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .ToListAsync();
            return Ok(categories);
        }

        // GET: api/category/{id} - دریافت دسته بندی با شناسه
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound(new { success = false, message = "دسته بندی یافت نشد" });

            return Ok(category);
        }

        // POST: api/category - افزودن دسته بندی جدید
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var category = new Category
            {
                Name = request.Name,
                Description = request.Description
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "دسته بندی با موفقیت اضافه شد", category });
        }

        // PUT: api/category/{id} - ویرایش دسته بندی
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound(new { success = false, message = "دسته بندی یافت نشد" });

            if (!string.IsNullOrEmpty(request.Name))
                category.Name = request.Name;

            if (request.Description != null)
                category.Description = request.Description;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "دسته بندی با موفقیت ویرایش شد", category });
        }

        // DELETE: api/category/{id} - حذف دسته بندی
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound(new { success = false, message = "دسته بندی یافت نشد" });

            // بررسی وجود محصول در این دسته
            if (category.Products != null && category.Products.Any())
                return BadRequest(new { success = false, message = "این دسته بندی دارای محصول است و قابل حذف نیست" });

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "دسته بندی با موفقیت حذف شد" });
        }
    }

    public class CreateCategoryRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    public class UpdateCategoryRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}