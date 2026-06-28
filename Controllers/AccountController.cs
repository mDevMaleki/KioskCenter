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
    public class AccountController : ControllerBase
    {
        private readonly CoffeeShopContext _context;

        public AccountController(CoffeeShopContext context)
        {
            _context = context;
        }

        // GET: api/account - فهرست کدینگ حساب‌ها
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] AccountType? type)
        {
            var query = _context.Accounts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.Name.Contains(search) || a.Code.Contains(search));

            if (type.HasValue)
                query = query.Where(a => a.Type == type.Value);

            var items = await query
                .OrderBy(a => a.Code)
                .Select(a => new
                {
                    a.Id,
                    a.Code,
                    a.Name,
                    a.Type,
                    a.ParentId,
                    a.IsGroup,
                    a.IsActive,
                    a.Balance
                })
                .ToListAsync();

            return Ok(items);
        }

        // GET: api/account/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                return NotFound(new { success = false, message = "حساب یافت نشد" });

            return Ok(account);
        }

        // POST: api/account - ایجاد حساب جدید
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AccountDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { success = false, message = "کد و نام حساب الزامی است" });

            var codeExists = await _context.Accounts.AnyAsync(a => a.Code == dto.Code);
            if (codeExists)
                return BadRequest(new { success = false, message = "این کد حساب قبلاً ثبت شده است" });

            if (dto.ParentId.HasValue)
            {
                var parent = await _context.Accounts.FindAsync(dto.ParentId.Value);
                if (parent == null)
                    return BadRequest(new { success = false, message = "حساب والد یافت نشد" });

                if (!parent.IsGroup)
                    return BadRequest(new { success = false, message = "حساب والد باید از نوع گروه (کل) باشد" });
            }

            var account = new Account
            {
                Code = dto.Code,
                Name = dto.Name,
                Type = dto.Type,
                ParentId = dto.ParentId,
                IsGroup = dto.IsGroup,
                IsActive = true,
                Balance = 0
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return Ok(account);
        }

        // PUT: api/account/{id} - ویرایش حساب
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AccountDto dto)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                return NotFound(new { success = false, message = "حساب یافت نشد" });

            if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { success = false, message = "کد و نام حساب الزامی است" });

            var codeExists = await _context.Accounts.AnyAsync(a => a.Code == dto.Code && a.Id != id);
            if (codeExists)
                return BadRequest(new { success = false, message = "این کد حساب قبلاً ثبت شده است" });

            if (dto.ParentId.HasValue)
            {
                if (dto.ParentId.Value == id)
                    return BadRequest(new { success = false, message = "حساب نمی‌تواند والد خودش باشد" });

                var parent = await _context.Accounts.FindAsync(dto.ParentId.Value);
                if (parent == null)
                    return BadRequest(new { success = false, message = "حساب والد یافت نشد" });

                if (!parent.IsGroup)
                    return BadRequest(new { success = false, message = "حساب والد باید از نوع گروه (کل) باشد" });
            }

            account.Code = dto.Code;
            account.Name = dto.Name;
            account.Type = dto.Type;
            account.ParentId = dto.ParentId;
            account.IsGroup = dto.IsGroup;
            account.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return Ok(account);
        }

        // DELETE: api/account/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                return NotFound(new { success = false, message = "حساب یافت نشد" });

            var hasChildren = await _context.Accounts.AnyAsync(a => a.ParentId == id);
            if (hasChildren)
                return BadRequest(new { success = false, message = "این حساب دارای زیرحساب است و قابل حذف نیست" });

            var hasLines = await _context.JournalEntryLines.AnyAsync(l => l.AccountId == id);
            if (hasLines)
                return BadRequest(new { success = false, message = "این حساب دارای گردش مالی است و قابل حذف نیست" });

            var hasCashAccount = await _context.CashAccounts.AnyAsync(c => c.AccountId == id);
            if (hasCashAccount)
                return BadRequest(new { success = false, message = "این حساب به یک صندوق/بانک متصل است و قابل حذف نیست" });

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "حساب با موفقیت حذف شد" });
        }

        // GET: api/account/{id}/ledger - دفتر حساب (گردش حساب)
        [HttpGet("{id}/ledger")]
        public async Task<IActionResult> GetLedger(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                return NotFound(new { success = false, message = "حساب یافت نشد" });

            var query = _context.JournalEntryLines
                .Where(l => l.AccountId == id)
                .Include(l => l.JournalEntry)
                .Include(l => l.Party)
                .Include(l => l.CashAccount);

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
                    PartyName = l.Party != null ? l.Party.Name : null,
                    CashAccountName = l.CashAccount != null ? l.CashAccount.Name : null
                })
                .ToListAsync();

            return Ok(new
            {
                accountId = account.Id,
                accountCode = account.Code,
                accountName = account.Name,
                balance = account.Balance,
                total,
                page,
                pageSize,
                items = lines
            });
        }

        // GET: api/account/trial-balance - تراز آزمایشی
        [HttpGet("trial-balance")]
        public async Task<IActionResult> GetTrialBalance()
        {
            var accounts = await _context.Accounts
                .OrderBy(a => a.Code)
                .Select(a => new
                {
                    a.Id,
                    a.Code,
                    a.Name,
                    a.Type,
                    a.ParentId,
                    a.IsGroup,
                    a.Balance
                })
                .ToListAsync();

            decimal totalDebit = 0;
            decimal totalCredit = 0;

            var items = accounts.Select(a =>
            {
                var debit = a.Balance > 0 ? a.Balance : 0;
                var credit = a.Balance < 0 ? -a.Balance : 0;

                if (!a.IsGroup)
                {
                    totalDebit += debit;
                    totalCredit += credit;
                }

                return new
                {
                    a.Id,
                    a.Code,
                    a.Name,
                    a.Type,
                    a.ParentId,
                    a.IsGroup,
                    debit,
                    credit
                };
            }).ToList();

            return Ok(new { items, totalDebit, totalCredit });
        }
    }

    public class AccountDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        public int? ParentId { get; set; }
        public bool IsGroup { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}
