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
    [Authorize, RequirePermission("purchase-sale")]
    public class PartyController : ControllerBase
    {
        private readonly CoffeeShopContext _context;
        private readonly JournalPostingService _postingService;

        public PartyController(CoffeeShopContext context, JournalPostingService postingService)
        {
            _context = context;
            _postingService = postingService;
        }

        // GET: api/party - لیست طرف‌های حساب
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] PartyType? type)
        {
            var query = _context.Parties.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search));

            if (type.HasValue)
                query = query.Where(p => p.Type == type.Value);

            var items = await query
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Type,
                    p.Phone,
                    p.Address,
                    p.Balance,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        // GET: api/party/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(int id)
        {
            var party = await _context.Parties.FindAsync(id);
            if (party == null)
                return NotFound(new { success = false, message = "طرف حساب یافت نشد" });

            return Ok(party);
        }

        // POST: api/party - ایجاد طرف حساب جدید
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PartyDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { success = false, message = "نام طرف حساب الزامی است" });

            var party = new Party
            {
                Name = dto.Name,
                Type = dto.Type,
                Phone = dto.Phone,
                Address = dto.Address,
                CreatedAt = DateTime.Now
            };

            _context.Parties.Add(party);
            await _context.SaveChangesAsync();

            return Ok(party);
        }

        // PUT: api/party/{id} - ویرایش طرف حساب
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PartyDto dto)
        {
            var party = await _context.Parties.FindAsync(id);
            if (party == null)
                return NotFound(new { success = false, message = "طرف حساب یافت نشد" });

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { success = false, message = "نام طرف حساب الزامی است" });

            party.Name = dto.Name;
            party.Type = dto.Type;
            party.Phone = dto.Phone;
            party.Address = dto.Address;

            await _context.SaveChangesAsync();

            return Ok(party);
        }

        // DELETE: api/party/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var party = await _context.Parties.FindAsync(id);
            if (party == null)
                return NotFound(new { success = false, message = "طرف حساب یافت نشد" });

            var hasTransactions = await _context.PartyTransactions.AnyAsync(t => t.PartyId == id);
            if (hasTransactions)
                return BadRequest(new { success = false, message = "این طرف حساب دارای تراکنش است و قابل حذف نیست" });

            _context.Parties.Remove(party);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "طرف حساب با موفقیت حذف شد" });
        }

        // GET: api/party/{id}/ledger - دفتر حساب (تراکنش‌ها)
        [HttpGet("{id}/ledger")]
        public async Task<IActionResult> GetLedger(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var party = await _context.Parties.FindAsync(id);
            if (party == null)
                return NotFound(new { success = false, message = "طرف حساب یافت نشد" });

            var query = _context.PartyTransactions.Where(t => t.PartyId == id);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    t.Id,
                    t.Type,
                    t.Amount,
                    t.BalanceAfter,
                    t.RefId,
                    t.Description,
                    t.CreatedAt
                })
                .ToListAsync();

            return Ok(new { partyId = party.Id, partyName = party.Name, balance = party.Balance, total, page, pageSize, items });
        }

        // POST: api/party/{id}/payment - ثبت پرداخت ما به طرف حساب (کاهش بدهی ما / افزایش مانده)
        [HttpPost("{id}/payment")]
        public async Task<IActionResult> Payment(int id, [FromBody] PartyPaymentRequest request)
        {
            return await RegisterCashTransaction(id, request, PartyTransactionType.Payment, request.Amount);
        }

        // POST: api/party/{id}/receipt - ثبت دریافت ما از طرف حساب (کاهش بدهی او / کاهش مانده)
        [HttpPost("{id}/receipt")]
        public async Task<IActionResult> Receipt(int id, [FromBody] PartyPaymentRequest request)
        {
            return await RegisterCashTransaction(id, request, PartyTransactionType.Receipt, -request.Amount);
        }

        private async Task<IActionResult> RegisterCashTransaction(int id, PartyPaymentRequest request, PartyTransactionType type, decimal signedAmount)
        {
            if (request.Amount <= 0)
                return BadRequest(new { success = false, message = "مقدار باید بیشتر از صفر باشد" });

            var party = await _context.Parties.FindAsync(id);
            if (party == null)
                return NotFound(new { success = false, message = "طرف حساب یافت نشد" });

            var cashAccount = await _context.CashAccounts.FindAsync(request.CashAccountId);
            if (cashAccount == null)
                return BadRequest(new { success = false, message = "صندوق/بانک انتخاب‌شده معتبر نیست" });

            party.Balance += signedAmount;

            var tx = new PartyTransaction
            {
                PartyId = party.Id,
                Type = type,
                Amount = signedAmount,
                BalanceAfter = party.Balance,
                Description = request.Description,
                CreatedAt = DateTime.Now
            };

            _context.PartyTransactions.Add(tx);
            await _context.SaveChangesAsync();

            // ثبت سند حسابداری
            var description = $"{(type == PartyTransactionType.Payment ? "پرداخت به" : "دریافت از")} {party.Name}" +
                (string.IsNullOrWhiteSpace(request.Description) ? "" : $" - {request.Description}");

            var lines = type == PartyTransactionType.Payment
                ? new List<JournalLineInput>
                {
                    new JournalLineInput { AccountId = AccountingConstants.AccountsPayable, Debit = request.Amount, Credit = 0, PartyId = party.Id },
                    new JournalLineInput { AccountId = cashAccount.AccountId, Debit = 0, Credit = request.Amount, CashAccountId = cashAccount.Id }
                }
                : new List<JournalLineInput>
                {
                    new JournalLineInput { AccountId = cashAccount.AccountId, Debit = request.Amount, Credit = 0, CashAccountId = cashAccount.Id },
                    new JournalLineInput { AccountId = AccountingConstants.AccountsReceivable, Debit = 0, Credit = request.Amount, PartyId = party.Id }
                };

            await _postingService.PostAsync(
                tx.CreatedAt,
                description,
                type == PartyTransactionType.Payment ? JournalEntryRefType.Payment : JournalEntryRefType.Receipt,
                tx.Id,
                lines);

            return Ok(new { success = true, message = "تراکنش با موفقیت ثبت شد", balance = party.Balance });
        }
    }

    public class PartyDto
    {
        public string Name { get; set; } = string.Empty;
        public PartyType Type { get; set; } = PartyType.Both;
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public class PartyPaymentRequest
    {
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public int CashAccountId { get; set; }
    }
}
