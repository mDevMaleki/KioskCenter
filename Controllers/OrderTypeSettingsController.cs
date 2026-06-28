using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using KioskCenter.Authorization;
using KioskCenter.Data;
using KioskCenter.Models;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderTypeSettingsController : ControllerBase
    {
        private readonly CoffeeShopContext _context;
        private readonly ILogger<OrderTypeSettingsController> _logger;

        public OrderTypeSettingsController(CoffeeShopContext context, ILogger<OrderTypeSettingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/OrderTypeSettings
        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                var settings = await _context.OrderTypeSettings.ToListAsync();
                var defaultSettings = GetDefaultSettings();

                foreach (var setting in settings)
                {
                    var dto = defaultSettings.FirstOrDefault(d => d.Id == setting.OrderTypeId);
                    if (dto != null && !string.IsNullOrEmpty(setting.SettingsJson))
                    {
                        try
                        {
                            var savedSettings = JsonSerializer.Deserialize<OrderTypeSettingDto>(setting.SettingsJson);
                            if (savedSettings != null)
                            {
                                dto.Active = savedSettings.Active;
                                dto.PriceFactor = savedSettings.PriceFactor;
                                dto.VisibleCategoryIds = savedSettings.VisibleCategoryIds;
                                dto.CategoryPriceRules = savedSettings.CategoryPriceRules;
                                if (!string.IsNullOrEmpty(savedSettings.PersianName))
                                    dto.PersianName = savedSettings.PersianName;
                                if (!string.IsNullOrEmpty(savedSettings.Icon))
                                    dto.Icon = savedSettings.Icon;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error deserializing settings for {OrderTypeId}", setting.OrderTypeId);
                        }
                    }
                }

                return Ok(defaultSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order type settings");
                return StatusCode(500, new { success = false, message = "خطا در دریافت تنظیمات" });
            }
        }

        // POST: api/OrderTypeSettings
        [Authorize, RequirePermission("order-type")]
        [HttpPost]
        public async Task<IActionResult> SaveSettings([FromBody] List<OrderTypeSettingDto> settings)
        {
            try
            {
                foreach (var setting in settings)
                {
                    var existing = await _context.OrderTypeSettings
                        .FirstOrDefaultAsync(s => s.OrderTypeId == setting.Id);

                    var settingsJson = JsonSerializer.Serialize(setting);

                    if (existing == null)
                    {
                        var newSetting = new OrderTypeSettings
                        {
                            OrderTypeId = setting.Id,
                            SettingsJson = settingsJson,
                            UpdatedAt = DateTime.UtcNow,
                            UpdatedBy = "Admin"
                        };
                        await _context.OrderTypeSettings.AddAsync(newSetting);
                    }
                    else
                    {
                        existing.SettingsJson = settingsJson;
                        existing.UpdatedAt = DateTime.UtcNow;
                        _context.OrderTypeSettings.Update(existing);
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "تنظیمات با موفقیت ذخیره شد" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving order type settings");
                return StatusCode(500, new { success = false, message = "خطا در ذخیره تنظیمات" });
            }
        }

        // GET: api/OrderTypeSettings/{orderTypeId}/categories
        [HttpGet("{orderTypeId}/categories")]
        public async Task<IActionResult> GetVisibleCategories(string orderTypeId)
        {
            try
            {
                var allCategories = await _context.Categories.ToListAsync();
                var settings = await GetSettingsForOrderType(orderTypeId);

                if (settings == null || !settings.Active || settings.VisibleCategoryIds.Count == 0)
                {
                    return Ok(allCategories);
                }

                var visibleCategories = allCategories
                    .Where(c => settings.VisibleCategoryIds.Contains(c.Id.ToString()))
                    .ToList();

                return Ok(visibleCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting visible categories");
                return StatusCode(500, new { success = false, message = "خطا در دریافت دسته‌بندی‌ها" });
            }
        }

        // POST: api/OrderTypeSettings/calculate-price
        [HttpPost("calculate-price")]
        public async Task<IActionResult> CalculatePrice([FromBody] PriceCalculationRequest request)
        {
            try
            {
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                    return NotFound(new { success = false, message = "محصول یافت نشد" });

                var settings = await GetSettingsForOrderType(request.OrderTypeId);
                if (settings == null || !settings.Active)
                {
                    return Ok(new { originalPrice = product.Price, finalPrice = product.Price });
                }

                decimal factor = settings.PriceFactor;

                // بررسی قانون اختصاصی برای دسته‌بندی
                if (request.CategoryId.HasValue)
                {
                    var categoryRule = settings.CategoryPriceRules
                        .FirstOrDefault(r => r.CategoryId == request.CategoryId.Value.ToString());
                    if (categoryRule != null)
                    {
                        factor = categoryRule.Factor;
                    }
                }

                var originalPrice = request.UseSecondPrice && product.SecondPrice.HasValue
                    ? product.SecondPrice.Value
                    : product.Price;

                var finalPrice = Math.Round(originalPrice * factor, 0);

                return Ok(new { originalPrice, finalPrice, factor });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating price");
                return StatusCode(500, new { success = false, message = "خطا در محاسبه قیمت" });
            }
        }

        private async Task<OrderTypeSettingDto?> GetSettingsForOrderType(string orderTypeId)
        {
            var setting = await _context.OrderTypeSettings
                .FirstOrDefaultAsync(s => s.OrderTypeId == orderTypeId);

            if (setting == null || string.IsNullOrEmpty(setting.SettingsJson))
                return null;

            return JsonSerializer.Deserialize<OrderTypeSettingDto>(setting.SettingsJson);
        }

        private List<OrderTypeSettingDto> GetDefaultSettings()
        {
            return new List<OrderTypeSettingDto>
            {
                new OrderTypeSettingDto
                {
                    Id = "EatIn",
                    Name = "EatIn",
                    PersianName = "داخل سالن",
                    Icon = "🏠",
                    Active = true,
                    PriceFactor = 1,
                    VisibleCategoryIds = new List<string>(),
                    CategoryPriceRules = new List<CategoryPriceRule>()
                },
                new OrderTypeSettingDto
                {
                    Id = "TakeAway",
                    Name = "TakeAway",
                    PersianName = "بیرون بر",
                    Icon = "🛍️",
                    Active = true,
                    PriceFactor = 1.1m,
                    VisibleCategoryIds = new List<string>(),
                    CategoryPriceRules = new List<CategoryPriceRule>()
                }
            };
        }
    }

    public class PriceCalculationRequest
    {
        public int ProductId { get; set; }
        public string OrderTypeId { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public bool UseSecondPrice { get; set; } = false;
    }
}