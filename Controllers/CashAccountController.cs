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
    [Authorize, RequirePermission("accounting")]
    public class CashAccountController : ControllerBase
    {
        private readonly CoffeeShopContext _context;

        public CashAccountController(CoffeeShopContext context)
        {
            _context = context;
        }

        // GET: api/cashaccount - فهرست صندوق‌ها و بانک‌ها
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly)
        {
            var query = _context.CashAccounts.AsQueryable();

            if (activeOnly == true)
                query = query.Where(c => c.IsActive);

            var items = await query
                .OrderBy(c => c.Id)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Type,
                    c.AccountNumber,
                    c.AccountId,
                    c.Balance,
                    c.IsActive,
                    c.CreatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        // GET: api/cashaccount/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(int id)
        {
            var cashAccount = await _context.CashAccounts.FindAsync(id);
            if (cashAccount == null)
                return NotFound(new { success = false, message = "صندوق/بانک یافت نشد" });

            return Ok(cashAccount);
        }

        // POST: api/cashaccount - ایجاد صندوق/بانک جدید
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CashAccountDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { success = false, message = "نام الزامی است" });

            var account = await _context.Accounts.FindAsync(dto.AccountId);
            if (account == null)
                return BadRequest(new { success = false, message = "حساب معین مرتبط یافت نشد" });

            if (account.IsGroup)
                return BadRequest(new { success = false, message = "حساب معین باید از نوع غیر گروه باشد" });

            var cashAccount = new CashAccount
            {
                Name = dto.Name,
                Type = dto.Type,
                AccountNumber = dto.AccountNumber,
                AccountId = dto.AccountId,
                Balance = 0,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.CashAccounts.Add(cashAccount);
            await _context.SaveChangesAsync();

            return Ok(cashAccount);
        }

        // PUT: api/cashaccount/{id} - ویرایش صندوق/بانک
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CashAccountDto dto)
        {
            var cashAccount = await _context.CashAccounts.FindAsync(id);
            if (cashAccount == null)
                return NotFound(new { success = false, message = "صندوق/بانک یافت نشد" });

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { success = false, message = "نام الزامی است" });

            var account = await _context.Accounts.FindAsync(dto.AccountId);
            if (account == null)
                return BadRequest(new { success = false, message = "حساب معین مرتبط یافت نشد" });

            if (account.IsGroup)
                return BadRequest(new { success = false, message = "حساب معین باید از نوع غیر گروه باشد" });

            cashAccount.Name = dto.Name;
            cashAccount.Type = dto.Type;
            cashAccount.AccountNumber = dto.AccountNumber;
            cashAccount.AccountId = dto.AccountId;
            cashAccount.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return Ok(cashAccount);
        }

        // DELETE: api/cashaccount/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cashAccount = await _context.CashAccounts.FindAsync(id);
            if (cashAccount == null)
                return NotFound(new { success = false, message = "صندوق/بانک یافت نشد" });

            var hasLines = await _context.JournalEntryLines.AnyAsync(l => l.CashAccountId == id);
            if (hasLines)
                return BadRequest(new { success = false, message = "این صندوق/بانک دارای گردش مالی است و قابل حذف نیست" });

            _context.CashAccounts.Remove(cashAccount);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "صندوق/بانک با موفقیت حذف شد" });
        }

        // GET: api/cashaccount/{id}/ledger - گردش صندوق/بانک
        [HttpGet("{id}/ledger")]
        public async Task<IActionResult> GetLedger(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var cashAccount = await _context.CashAccounts.FindAsync(id);
            if (cashAccount == null)
                return NotFound(new { success = false, message = "صندوق/بانک یافت نشد" });

            var query = _context.JournalEntryLines
                .Where(l => l.CashAccountId == id)
                .Include(l => l.JournalEntry)
                .Include(l => l.Party);

            var total = await query.CountAsync();

            var lines = await query
                .OrderByDescending(l => l.JournalEntry!.EntryDate)
                .ThenByDescending(l => l.JournalEntryId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.JournalEntryId,
                    JournalEntryNumber = l.JournalEntry!.Number,
                    EntryDate = l.JournalEntry!.EntryDate,
                    l.JournalEntry!.RefType,
                    l.JournalEntry!.RefId,
                    l.Debit,
                    l.Credit,
                    l.Description,
                    PartyName = l.Party != null ? l.Party.Name : null
                })
                .ToListAsync();

            return Ok(new
            {
                cashAccountId = cashAccount.Id,
                cashAccountName = cashAccount.Name,
                balance = cashAccount.Balance,
                total,
                page,
                pageSize,
                items = lines
            });
        }
    }

    public class CashAccountDto
    {
        public string Name { get; set; } = string.Empty;
        public CashAccountType Type { get; set; } = CashAccountType.Cash;
        public string? AccountNumber { get; set; }
        public int AccountId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
