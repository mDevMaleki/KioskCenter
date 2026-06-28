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
    public class PettyCashController : ControllerBase
    {
        private readonly CoffeeShopContext _context;
        private readonly JournalPostingService _postingService;

        private const string PettyCashControlCode = "1500"; // تنخواه‌گردان

        public PettyCashController(CoffeeShopContext context, JournalPostingService postingService)
        {
            _context = context;
            _postingService = postingService;
        }

        // GET: api/pettycash/funds
        [HttpGet("funds")]
        public async Task<IActionResult> GetFunds()
        {
            var funds = await _context.PettyCashFunds
                .Include(f => f.SourceCashAccount)
                .Where(f => f.IsActive)
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.Custodian,
                    f.Balance,
                    f.SourceCashAccountId,
                    SourceCashAccountName = f.SourceCashAccount != null ? f.SourceCashAccount.Name : null
                })
                .ToListAsync();

            return Ok(funds);
        }

        // POST: api/pettycash/funds
        [HttpPost("funds")]
        public async Task<IActionResult> CreateFund([FromBody] PettyCashFundDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.SourceCashAccountId <= 0)
                return BadRequest(new { success = false, message = "نام و صندوق/بانک منبع الزامی است" });

            var fund = new PettyCashFund
            {
                Name = dto.Name,
                Custodian = dto.Custodian,
                SourceCashAccountId = dto.SourceCashAccountId,
                Balance = 0,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.PettyCashFunds.Add(fund);
            await _context.SaveChangesAsync();
            return Ok(fund);
        }

        // GET: api/pettycash/funds/{id}/transactions
        [HttpGet("funds/{id}/transactions")]
        public async Task<IActionResult> GetTransactions(int id)
        {
            var txs = await _context.PettyCashTransactions
                .Include(t => t.ExpenseAccount)
                .Where(t => t.PettyCashFundId == id)
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new
                {
                    t.Id,
                    t.Type,
                    t.Amount,
                    t.Description,
                    t.TransactionDate,
                    ExpenseAccountName = t.ExpenseAccount != null ? t.ExpenseAccount.Name : null
                })
                .ToListAsync();

            return Ok(txs);
        }

        // POST: api/pettycash/funds/{id}/replenish - شارژ تنخواه
        [HttpPost("funds/{id}/replenish")]
        public async Task<IActionResult> Replenish(int id, [FromBody] PettyCashReplenishDto dto)
        {
            var fund = await _context.PettyCashFunds.Include(f => f.SourceCashAccount).FirstOrDefaultAsync(f => f.Id == id);
            if (fund == null) return NotFound();
            if (dto.Amount <= 0) return BadRequest(new { success = false, message = "مبلغ نامعتبر است" });

            var pettyCashAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == PettyCashControlCode);
            if (pettyCashAccount == null) return BadRequest(new { success = false, message = "حساب کنترل تنخواه یافت نشد" });

            var sourceAccountId = fund.SourceCashAccount!.AccountId;

            var lines = new List<JournalLineInput>
            {
                new JournalLineInput { AccountId = pettyCashAccount.Id, Debit = dto.Amount, Description = $"شارژ تنخواه {fund.Name}" },
                new JournalLineInput { AccountId = sourceAccountId, Credit = dto.Amount, Description = $"شارژ تنخواه {fund.Name}", CashAccountId = fund.SourceCashAccountId }
            };

            var entry = await _postingService.PostAsync(DateTime.Now, $"شارژ تنخواه {fund.Name}", JournalEntryRefType.PettyCash, fund.Id, lines);

            fund.Balance += dto.Amount;

            _context.PettyCashTransactions.Add(new PettyCashTransaction
            {
                PettyCashFundId = fund.Id,
                Type = PettyCashTransactionType.Replenish,
                Amount = dto.Amount,
                Description = dto.Description,
                TransactionDate = DateTime.Now,
                JournalEntryId = entry.Id
            });

            await _context.SaveChangesAsync();

            return Ok(new { success = true, balance = fund.Balance });
        }

        // POST: api/pettycash/funds/{id}/spend - مصرف تنخواه
        [HttpPost("funds/{id}/spend")]
        public async Task<IActionResult> Spend(int id, [FromBody] PettyCashSpendDto dto)
        {
            var fund = await _context.PettyCashFunds.FindAsync(id);
            if (fund == null) return NotFound();
            if (dto.Amount <= 0) return BadRequest(new { success = false, message = "مبلغ نامعتبر است" });
            if (dto.Amount > fund.Balance) return BadRequest(new { success = false, message = "موجودی تنخواه کافی نیست" });

            var expenseAccount = await _context.Accounts.FindAsync(dto.ExpenseAccountId);
            if (expenseAccount == null) return BadRequest(new { success = false, message = "حساب هزینه یافت نشد" });

            var pettyCashAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == PettyCashControlCode);
            if (pettyCashAccount == null) return BadRequest(new { success = false, message = "حساب کنترل تنخواه یافت نشد" });

            var lines = new List<JournalLineInput>
            {
                new JournalLineInput { AccountId = dto.ExpenseAccountId, Debit = dto.Amount, Description = dto.Description ?? $"مصرف تنخواه {fund.Name}" },
                new JournalLineInput { AccountId = pettyCashAccount.Id, Credit = dto.Amount, Description = dto.Description ?? $"مصرف تنخواه {fund.Name}" }
            };

            var entry = await _postingService.PostAsync(DateTime.Now, $"مصرف تنخواه {fund.Name}", JournalEntryRefType.PettyCash, fund.Id, lines);

            fund.Balance -= dto.Amount;

            _context.PettyCashTransactions.Add(new PettyCashTransaction
            {
                PettyCashFundId = fund.Id,
                Type = PettyCashTransactionType.Spend,
                Amount = dto.Amount,
                ExpenseAccountId = dto.ExpenseAccountId,
                Description = dto.Description,
                TransactionDate = DateTime.Now,
                JournalEntryId = entry.Id
            });

            await _context.SaveChangesAsync();

            return Ok(new { success = true, balance = fund.Balance });
        }
    }

    public class PettyCashFundDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Custodian { get; set; }
        public int SourceCashAccountId { get; set; }
    }

    public class PettyCashReplenishDto
    {
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }

    public class PettyCashSpendDto
    {
        public decimal Amount { get; set; }
        public int ExpenseAccountId { get; set; }
        public string? Description { get; set; }
    }
}
