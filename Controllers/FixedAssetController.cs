using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KioskCenter.Authorization;
using KioskCenter.Data;
using KioskCenter.Models;
using KioskCenter.Services;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize, RequirePermission("accounting")]
    public class FixedAssetController : ControllerBase
    {
        private readonly CoffeeShopContext _context;
        private readonly JournalPostingService _postingService;

        public FixedAssetController(CoffeeShopContext context, JournalPostingService postingService)
        {
            _context = context;
            _postingService = postingService;
        }

        // GET: api/fixedasset
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _context.FixedAssets
                .Where(f => f.IsActive)
                .OrderBy(f => f.Name)
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.PurchaseDate,
                    f.PurchaseValue,
                    f.SalvageValue,
                    f.UsefulLifeMonths,
                    f.AccumulatedDepreciation,
                    BookValue = f.PurchaseValue - f.AccumulatedDepreciation,
                    f.AssetAccountId,
                    f.DepreciationExpenseAccountId,
                    f.AccumulatedDepreciationAccountId
                })
                .ToListAsync();

            return Ok(items);
        }

        // GET: api/fixedasset/{id}/history
        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetHistory(int id)
        {
            var records = await _context.DepreciationRecords
                .Where(r => r.FixedAssetId == id)
                .OrderByDescending(r => r.PeriodDate)
                .ToListAsync();

            return Ok(records);
        }

        // POST: api/fixedasset
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FixedAssetDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.PurchaseValue <= 0 || dto.UsefulLifeMonths <= 0)
                return BadRequest(new { success = false, message = "نام، ارزش خرید و عمر مفید الزامی است" });

            var assetAccount = await _context.Accounts.FindAsync(dto.AssetAccountId);
            var expenseAccount = await _context.Accounts.FindAsync(dto.DepreciationExpenseAccountId);
            var accumAccount = await _context.Accounts.FindAsync(dto.AccumulatedDepreciationAccountId);

            if (assetAccount == null || expenseAccount == null || accumAccount == null)
                return BadRequest(new { success = false, message = "حساب‌های مرتبط یافت نشد" });

            var asset = new FixedAsset
            {
                Name = dto.Name,
                PurchaseDate = dto.PurchaseDate,
                PurchaseValue = dto.PurchaseValue,
                SalvageValue = dto.SalvageValue,
                UsefulLifeMonths = dto.UsefulLifeMonths,
                AssetAccountId = dto.AssetAccountId,
                DepreciationExpenseAccountId = dto.DepreciationExpenseAccountId,
                AccumulatedDepreciationAccountId = dto.AccumulatedDepreciationAccountId,
                AccumulatedDepreciation = 0,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.FixedAssets.Add(asset);
            await _context.SaveChangesAsync();

            // ثبت سند خرید دارایی: بدهکار دارایی ثابت / بستانکار صندوق یا حساب پرداختنی (دستی توسط کاربر در صورت نیاز انجام می‌شود)

            return Ok(asset);
        }

        // PUT: api/fixedasset/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] FixedAssetDto dto)
        {
            var asset = await _context.FixedAssets.FindAsync(id);
            if (asset == null) return NotFound();

            asset.Name = dto.Name;
            asset.PurchaseDate = dto.PurchaseDate;
            asset.PurchaseValue = dto.PurchaseValue;
            asset.SalvageValue = dto.SalvageValue;
            asset.UsefulLifeMonths = dto.UsefulLifeMonths;

            await _context.SaveChangesAsync();
            return Ok(asset);
        }

        // DELETE: api/fixedasset/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var asset = await _context.FixedAssets.FindAsync(id);
            if (asset == null) return NotFound();

            var hasDepreciation = await _context.DepreciationRecords.AnyAsync(r => r.FixedAssetId == id);
            if (hasDepreciation)
                return BadRequest(new { success = false, message = "این دارایی استهلاک ثبت‌شده دارد و قابل حذف نیست؛ غیرفعال کنید" });

            _context.FixedAssets.Remove(asset);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // POST: api/fixedasset/{id}/run-depreciation - محاسبه و ثبت استهلاک ماهانه (خطی)
        [HttpPost("{id}/run-depreciation")]
        public async Task<IActionResult> RunDepreciation(int id, [FromBody] RunDepreciationDto dto)
        {
            var asset = await _context.FixedAssets.FindAsync(id);
            if (asset == null) return NotFound();

            var depreciableBase = asset.PurchaseValue - asset.SalvageValue;
            var monthlyAmount = Math.Round(depreciableBase / asset.UsefulLifeMonths, 2);

            var remaining = depreciableBase - asset.AccumulatedDepreciation;
            if (remaining <= 0)
                return BadRequest(new { success = false, message = "این دارایی به‌طور کامل مستهلک شده است" });

            var amount = Math.Min(monthlyAmount, remaining);

            var lines = new List<JournalLineInput>
            {
                new JournalLineInput { AccountId = asset.DepreciationExpenseAccountId, Debit = amount, Description = $"استهلاک دارایی {asset.Name}" },
                new JournalLineInput { AccountId = asset.AccumulatedDepreciationAccountId, Credit = amount, Description = $"استهلاک دارایی {asset.Name}" }
            };

            var entry = await _postingService.PostAsync(dto.PeriodDate, $"استهلاک ماهانه - {asset.Name}", JournalEntryRefType.FixedAssetDepreciation, asset.Id, lines);

            asset.AccumulatedDepreciation += amount;

            _context.DepreciationRecords.Add(new DepreciationRecord
            {
                FixedAssetId = asset.Id,
                PeriodDate = dto.PeriodDate,
                Amount = amount,
                JournalEntryId = entry.Id,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "استهلاک با موفقیت ثبت شد", amount, bookValue = asset.PurchaseValue - asset.AccumulatedDepreciation });
        }

        // POST: api/fixedasset/run-depreciation-all - استهلاک همه دارایی‌های فعال برای یک دوره
        [HttpPost("run-depreciation-all")]
        public async Task<IActionResult> RunDepreciationAll([FromBody] RunDepreciationDto dto)
        {
            var assets = await _context.FixedAssets.Where(f => f.IsActive).ToListAsync();
            var results = new List<object>();

            foreach (var asset in assets)
            {
                var depreciableBase = asset.PurchaseValue - asset.SalvageValue;
                var remaining = depreciableBase - asset.AccumulatedDepreciation;
                if (remaining <= 0) continue;

                var monthlyAmount = Math.Round(depreciableBase / asset.UsefulLifeMonths, 2);
                var amount = Math.Min(monthlyAmount, remaining);

                var lines = new List<JournalLineInput>
                {
                    new JournalLineInput { AccountId = asset.DepreciationExpenseAccountId, Debit = amount, Description = $"استهلاک دارایی {asset.Name}" },
                    new JournalLineInput { AccountId = asset.AccumulatedDepreciationAccountId, Credit = amount, Description = $"استهلاک دارایی {asset.Name}" }
                };

                var entry = await _postingService.PostAsync(dto.PeriodDate, $"استهلاک ماهانه - {asset.Name}", JournalEntryRefType.FixedAssetDepreciation, asset.Id, lines);

                asset.AccumulatedDepreciation += amount;

                _context.DepreciationRecords.Add(new DepreciationRecord
                {
                    FixedAssetId = asset.Id,
                    PeriodDate = dto.PeriodDate,
                    Amount = amount,
                    JournalEntryId = entry.Id,
                    CreatedAt = DateTime.Now
                });

                results.Add(new { asset.Id, asset.Name, amount });
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, count = results.Count, results });
        }
    }

    public class FixedAssetDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        public decimal PurchaseValue { get; set; }
        public decimal SalvageValue { get; set; } = 0;
        public int UsefulLifeMonths { get; set; }
        public int AssetAccountId { get; set; }
        public int DepreciationExpenseAccountId { get; set; }
        public int AccumulatedDepreciationAccountId { get; set; }
    }

    public class RunDepreciationDto
    {
        public DateTime PeriodDate { get; set; } = DateTime.Now;
    }
}
