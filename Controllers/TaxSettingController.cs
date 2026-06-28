using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KioskCenter.Authorization;
using KioskCenter.Data;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize, RequirePermission("accounting")]
    public class TaxSettingController : ControllerBase
    {
        private readonly CoffeeShopContext _context;

        public TaxSettingController(CoffeeShopContext context)
        {
            _context = context;
        }

        // GET: api/taxsetting
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var setting = await _context.TaxSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new Models.TaxSetting { VatRate = 9, IsEnabled = true };
                _context.TaxSettings.Add(setting);
                await _context.SaveChangesAsync();
            }
            return Ok(setting);
        }

        // PUT: api/taxsetting
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] TaxSettingDto dto)
        {
            var setting = await _context.TaxSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new Models.TaxSetting();
                _context.TaxSettings.Add(setting);
            }

            setting.VatRate = dto.VatRate;
            setting.IsEnabled = dto.IsEnabled;

            await _context.SaveChangesAsync();
            return Ok(setting);
        }
    }

    public class TaxSettingDto
    {
        public decimal VatRate { get; set; }
        public bool IsEnabled { get; set; }
    }
}
