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
    public class JournalEntryController : ControllerBase
    {
        private readonly CoffeeShopContext _context;
        private readonly JournalPostingService _postingService;

        public JournalEntryController(CoffeeShopContext context, JournalPostingService postingService)
        {
            _context = context;
            _postingService = postingService;
        }

        // GET: api/journalentry - فهرست اسناد حسابداری
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] JournalEntryRefType? refType,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _context.JournalEntries.AsQueryable();

            if (from.HasValue)
                query = query.Where(e => e.EntryDate >= from.Value);

            if (to.HasValue)
                query = query.Where(e => e.EntryDate <= to.Value);

            if (refType.HasValue)
                query = query.Where(e => e.RefType == refType.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(e => e.EntryDate)
                .ThenByDescending(e => e.Number)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new
                {
                    e.Id,
                    e.Number,
                    e.EntryDate,
                    e.Description,
                    e.RefType,
                    e.RefId,
                    TotalDebit = e.Lines!.Sum(l => l.Debit),
                    TotalCredit = e.Lines!.Sum(l => l.Credit)
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        // GET: api/journalentry/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(int id)
        {
            var entry = await _context.JournalEntries
                .Include(e => e.Lines)!.ThenInclude(l => l.Account)
                .Include(e => e.Lines)!.ThenInclude(l => l.Party)
                .Include(e => e.Lines)!.ThenInclude(l => l.CashAccount)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (entry == null)
                return NotFound(new { success = false, message = "سند حسابداری یافت نشد" });

            var result = new
            {
                entry.Id,
                entry.Number,
                entry.EntryDate,
                entry.Description,
                entry.RefType,
                entry.RefId,
                Lines = entry.Lines!.Select(l => new
                {
                    l.Id,
                    l.AccountId,
                    AccountCode = l.Account?.Code,
                    AccountName = l.Account?.Name,
                    l.Debit,
                    l.Credit,
                    l.Description,
                    l.PartyId,
                    PartyName = l.Party?.Name,
                    l.CashAccountId,
                    CashAccountName = l.CashAccount?.Name
                })
            };

            return Ok(result);
        }

        // POST: api/journalentry - ثبت سند حسابداری دستی
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JournalEntryDto dto)
        {
            if (dto.Lines == null || dto.Lines.Count < 2)
                return BadRequest(new { success = false, message = "سند حسابداری باید حداقل دو ردیف داشته باشد" });

            try
            {
                var lines = dto.Lines.Select(l => new JournalLineInput
                {
                    AccountId = l.AccountId,
                    Debit = l.Debit,
                    Credit = l.Credit,
                    Description = l.Description,
                    PartyId = l.PartyId,
                    CashAccountId = l.CashAccountId
                }).ToList();

                var entry = await _postingService.PostAsync(
                    dto.EntryDate == default ? DateTime.Now : dto.EntryDate,
                    dto.Description,
                    JournalEntryRefType.Manual,
                    null,
                    lines);

                return Ok(new { success = true, message = "سند حسابداری با موفقیت ثبت شد", id = entry.Id, number = entry.Number });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // DELETE: api/journalentry/{id} - حذف سند دستی (و اصلاح مانده‌ها)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entry = await _context.JournalEntries.FindAsync(id);
            if (entry == null)
                return NotFound(new { success = false, message = "سند حسابداری یافت نشد" });

            if (entry.RefType != JournalEntryRefType.Manual)
                return BadRequest(new { success = false, message = "فقط اسناد دستی قابل حذف هستند" });

            await _postingService.ReverseAsync(id);

            return Ok(new { success = true, message = "سند حسابداری با موفقیت حذف شد" });
        }
    }

    public class JournalEntryDto
    {
        public DateTime EntryDate { get; set; }
        public string? Description { get; set; }
        public List<JournalEntryLineDto> Lines { get; set; } = new();
    }

    public class JournalEntryLineDto
    {
        public int AccountId { get; set; }
        public decimal Debit { get; set; } = 0;
        public decimal Credit { get; set; } = 0;
        public string? Description { get; set; }
        public int? PartyId { get; set; }
        public int? CashAccountId { get; set; }
    }
}
