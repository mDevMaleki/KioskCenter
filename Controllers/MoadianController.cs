using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KioskCenter.Authorization;
using KioskCenter.Data;
using KioskCenter.Services;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize, RequirePermission("accounting")]
    public class MoadianController : ControllerBase
    {
        private readonly CoffeeShopContext _context;
        private readonly MoadianService _moadianService;

        public MoadianController(CoffeeShopContext context, MoadianService moadianService)
        {
            _context = context;
            _moadianService = moadianService;
        }

        // GET: api/moadian/settings
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var setting = await _moadianService.GetSettingsAsync();
            return Ok(new
            {
                setting.Id,
                setting.IsEnabled,
                setting.OrgKeyId,
                setting.Username,
                setting.BaseUrl,
                setting.LastUpdatedAt,
                HasKeys = !string.IsNullOrWhiteSpace(setting.PublicKeyPem) && !string.IsNullOrWhiteSpace(setting.PrivateKeyPem)
            });
        }

        // PUT: api/moadian/settings - فقط با تیک صریح کاربر (IsEnabled) فعال می‌شود
        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] MoadianSettingDto dto)
        {
            var setting = await _context.MoadianSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new Models.MoadianSetting();
                _context.MoadianSettings.Add(setting);
            }

            // IsEnabled فقط مستقیماً از تیک کاربر در رابط کاربری خوانده می‌شود؛ هیچ مقدار پیش‌فرضی فعال‌سازی نمی‌کند
            setting.IsEnabled = dto.IsEnabled;
            setting.OrgKeyId = dto.OrgKeyId;
            setting.Username = dto.Username;
            setting.BaseUrl = string.IsNullOrWhiteSpace(dto.BaseUrl) ? "https://tp.tax.gov.ir" : dto.BaseUrl;

            if (!string.IsNullOrWhiteSpace(dto.PublicKeyPem))
                setting.PublicKeyPem = dto.PublicKeyPem;

            if (!string.IsNullOrWhiteSpace(dto.PrivateKeyPem))
                setting.PrivateKeyPem = dto.PrivateKeyPem;

            setting.LastUpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "تنظیمات سامانه مودیان ذخیره شد" });
        }

        // POST: api/moadian/send/{saleInvoiceId} - ارسال دستی فاکتور فروش
        [HttpPost("send/{saleInvoiceId}")]
        public async Task<IActionResult> SendInvoice(int saleInvoiceId)
        {
            var setting = await _moadianService.GetSettingsAsync();
            if (!setting.IsEnabled)
                return BadRequest(new { success = false, message = "اتصال به سامانه مودیان فعال نیست. ابتدا در تنظیمات آن را فعال کنید" });

            var result = await _moadianService.TrySendSaleInvoiceAsync(saleInvoiceId);

            var invoice = await _context.SaleInvoices.FindAsync(saleInvoiceId);

            if (result)
                return Ok(new { success = true, message = "فاکتور با موفقیت به سامانه مودیان ارسال شد", referenceNumber = invoice?.MoadianReferenceNumber });

            return BadRequest(new { success = false, message = invoice?.MoadianError ?? "ارسال فاکتور ناموفق بود" });
        }
    }

    public class MoadianSettingDto
    {
        public bool IsEnabled { get; set; }
        public string? PublicKeyPem { get; set; }
        public string? PrivateKeyPem { get; set; }
        public string? OrgKeyId { get; set; }
        public string? Username { get; set; }
        public string? BaseUrl { get; set; }
    }
}
