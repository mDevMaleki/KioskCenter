using System.Linq;
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
    public class StyleController : ControllerBase
    {
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private static readonly string[] AllowedImageContentTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
        private const long MaxImageSize = 5 * 1024 * 1024; // 5MB

        private readonly CoffeeShopContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<StyleController> _logger;

        public StyleController(CoffeeShopContext context, IWebHostEnvironment env, ILogger<StyleController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var style = await _context.RestaurantStyles.FirstOrDefaultAsync();
            if (style == null)
            {
                style = CreateDefaultStyle();
                _context.RestaurantStyles.Add(style);
                await _context.SaveChangesAsync();
            }
            return Ok(style);
        }

        [Authorize, RequirePermission("style")]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] RestaurantStyle style)
        {
            var existing = await _context.RestaurantStyles.FirstOrDefaultAsync();
            if (existing == null)
            {
                return NotFound(new { success = false, message = "تنظیمات استایل یافت نشد" });
            }

            // اطلاعات رستوران
            existing.RestaurantName = style.RestaurantName;
            existing.LogoUrl = style.LogoUrl;

            // رنگ‌های اصلی
            existing.PrimaryColor = style.PrimaryColor;
            existing.SecondaryColor = style.SecondaryColor;
            existing.AccentColor = style.AccentColor;
            existing.BackgroundColor = style.BackgroundColor;
            existing.TextColor = style.TextColor;
            existing.TextSecondaryColor = style.TextSecondaryColor;

            // رنگ‌های دکمه‌ها
            existing.ButtonColor = style.ButtonColor;
            existing.ButtonHoverColor = style.ButtonHoverColor;
            existing.ButtonTextColor = style.ButtonTextColor;

            // رنگ‌های کارت محصولات
            existing.ProductCardBgColor = style.ProductCardBgColor;
            existing.ProductCardHoverColor = style.ProductCardHoverColor;
            existing.ProductCardBorderRadius = style.ProductCardBorderRadius;

            // رنگ‌های سایدبار
            existing.SidebarBgColor = style.SidebarBgColor;
            existing.SidebarHeaderBgColor = style.SidebarHeaderBgColor;
            existing.SidebarItemBgColor = style.SidebarItemBgColor;
            existing.SidebarItemHoverColor = style.SidebarItemHoverColor;

            // رنگ‌های دسته‌بندی
            existing.CategoryBtnBgColor = style.CategoryBtnBgColor;
            existing.CategoryBtnActiveBgColor = style.CategoryBtnActiveBgColor;
            existing.CategoryBtnTextColor = style.CategoryBtnTextColor;
            existing.CategoryBtnActiveTextColor = style.CategoryBtnActiveTextColor;

            // رنگ‌های نوع سفارش
            existing.OrderTypeCardBgColor = style.OrderTypeCardBgColor;
            existing.OrderTypeCardHoverColor = style.OrderTypeCardHoverColor;
            existing.OrderTypeCardTextColor = style.OrderTypeCardTextColor;

            // تنظیمات فونت
            existing.FontFamily = style.FontFamily;
            existing.CustomFontUrl = style.CustomFontUrl;
            existing.FontName = string.IsNullOrWhiteSpace(style.FontName) ? "YekanBakh" : style.FontName;
            existing.FontWeight = string.IsNullOrWhiteSpace(style.FontWeight) ? "500" : style.FontWeight;
            existing.FontStyle = string.IsNullOrWhiteSpace(style.FontStyle) ? "normal" : style.FontStyle;
            existing.FontSizeBase = style.FontSizeBase;
            existing.FontSizeTitle = style.FontSizeTitle;

            // بک‌گراند
            existing.BackgroundImage = style.BackgroundImage;
            existing.BackgroundOverlay = style.BackgroundOverlay;

            // اطلاعات تماس
            existing.FooterText = style.FooterText;
            existing.FooterBgColor = style.FooterBgColor;
            existing.FooterTextColor = style.FooterTextColor;
            existing.Address = style.Address;
            existing.Phone = style.Phone;
            existing.Instagram = style.Instagram;
            existing.WorkingHours = style.WorkingHours;

            // حاشیه‌ها
            existing.CardBorderRadius = style.CardBorderRadius;
            existing.ButtonBorderRadius = style.ButtonBorderRadius;
            existing.SpacingBase = style.SpacingBase;

            // انیمیشن‌ها
            existing.EnableAnimations = style.EnableAnimations;
            existing.AnimationDuration = style.AnimationDuration;

            existing.IsActive = style.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [Authorize, RequirePermission("style")]
        [HttpPost("upload-font")]
        public async Task<IActionResult> UploadFont(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "فایل انتخاب نشده است" });

            var allowedExtensions = new[] { ".ttf", ".otf", ".woff", ".woff2" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { success = false, message = "فرمت فایل پشتیبانی نمی‌شود. فقط TTF, OTF, WOFF, WOFF2" });

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "fonts");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fontUrl = $"/uploads/fonts/{uniqueFileName}";
            return Ok(new { success = true, url = fontUrl });
        }

        [Authorize, RequirePermission("style")]
        [HttpPost("upload-logo")]
        [RequestSizeLimit(MaxImageSize)]
        public async Task<IActionResult> UploadLogo(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "فایل انتخاب نشده است" });

            if (file.Length > MaxImageSize)
                return BadRequest(new { success = false, message = "حجم فایل بیشتر از حد مجاز است" });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
                return BadRequest(new { success = false, message = "فرمت فایل مجاز نیست" });

            if (!AllowedImageContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                return BadRequest(new { success = false, message = "نوع فایل مجاز نیست" });

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "logos");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var logoUrl = $"/uploads/logos/{uniqueFileName}";
            return Ok(new { success = true, url = logoUrl });
        }

        [Authorize, RequirePermission("style")]
        [HttpPost("upload-background")]
        [RequestSizeLimit(MaxImageSize)]
        public async Task<IActionResult> UploadBackground(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "فایل انتخاب نشده است" });

            if (file.Length > MaxImageSize)
                return BadRequest(new { success = false, message = "حجم فایل بیشتر از حد مجاز است" });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
                return BadRequest(new { success = false, message = "فرمت فایل مجاز نیست" });

            if (!AllowedImageContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                return BadRequest(new { success = false, message = "نوع فایل مجاز نیست" });

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "backgrounds");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var bgUrl = $"/uploads/backgrounds/{uniqueFileName}";
            return Ok(new { success = true, url = bgUrl });
        }

        [Authorize, RequirePermission("style")]
        [HttpPost("upload-order-type-image")]
        [RequestSizeLimit(MaxImageSize)]
        public async Task<IActionResult> UploadOrderTypeImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "فایل انتخاب نشده است" });

            if (file.Length > MaxImageSize)
                return BadRequest(new { success = false, message = "حجم فایل بیشتر از حد مجاز است" });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
                return BadRequest(new { success = false, message = "فرمت فایل مجاز نیست" });

            if (!AllowedImageContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                return BadRequest(new { success = false, message = "نوع فایل مجاز نیست" });

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "ordertypes");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var imageUrl = $"/uploads/ordertypes/{uniqueFileName}";
            return Ok(new { success = true, url = imageUrl });
        }

        private RestaurantStyle CreateDefaultStyle()
        {
            return new RestaurantStyle
            {
                RestaurantName = "کافی شاپ آغادون",
                LogoUrl = "/images/logo.png",
                PrimaryColor = "#2b1810",
                SecondaryColor = "#8d6e63",
                AccentColor = "#d4a574",
                BackgroundColor = "linear-gradient(135deg, #1a1a1a 0%, #2b2320 100%)",
                TextColor = "#ffffff",
                TextSecondaryColor = "rgba(255,255,255,0.7)",
                ButtonColor = "#6f4e37",
                ButtonHoverColor = "#8d6e63",
                ButtonTextColor = "#ffffff",
                ProductCardBgColor = "#2b2320",
                ProductCardHoverColor = "#3d322c",
                ProductCardBorderRadius = "24px",
                SidebarBgColor = "linear-gradient(180deg, #1a1a1a 0%, #2b1810 100%)",
                SidebarHeaderBgColor = "rgba(0,0,0,0.4)",
                SidebarItemBgColor = "rgba(255,255,255,0.05)",
                SidebarItemHoverColor = "rgba(255,255,255,0.1)",
                CategoryBtnBgColor = "#3d322c",
                CategoryBtnActiveBgColor = "#6f4e37",
                CategoryBtnTextColor = "#d4a574",
                CategoryBtnActiveTextColor = "#ffffff",
                OrderTypeCardBgColor = "#2b2320",
                OrderTypeCardHoverColor = "#3d322c",
                OrderTypeCardTextColor = "#ffffff",
                FontFamily = "YekanBakh, Tahoma, sans-serif",
                CustomFontUrl = "",
                FontName = "YekanBakh",
                FontWeight = "500",
                FontStyle = "normal",
                FontSizeBase = "14px",
                FontSizeTitle = "24px",
                BackgroundImage = "",
                BackgroundOverlay = "rgba(0,0,0,0.5)",
                FooterText = "",
                FooterBgColor = "rgba(0,0,0,0.85)",
                FooterTextColor = "#d4a574",
                Address = "",
                Phone = "",
                Instagram = "",
                WorkingHours = "",
                CardBorderRadius = "20px",
                ButtonBorderRadius = "8px",
                SpacingBase = "16px",
                EnableAnimations = true,
                AnimationDuration = "0.3s",
                IsActive = true,
                UpdatedAt = DateTime.Now
            };
        }
    }
}