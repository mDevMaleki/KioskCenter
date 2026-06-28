using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KioskCenter.Authorization;
using KioskCenter.Data;
using KioskCenter.Models;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/financialreport")]
    [Authorize]
    [RequirePermission("accounting")]
    public class FinancialReportController : ControllerBase
    {
        private readonly CoffeeShopContext _context;

        public FinancialReportController(CoffeeShopContext context)
        {
            _context = context;
        }

        public class LedgerLineDto
        {
            public int JournalEntryId { get; set; }
            public int Number { get; set; }
            public DateTime EntryDate { get; set; }
            public string? Description { get; set; }
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public decimal RunningBalance { get; set; }
        }

        public class AccountLedgerDto
        {
            public int AccountId { get; set; }
            public string AccountCode { get; set; } = string.Empty;
            public string AccountName { get; set; } = string.Empty;
            public decimal OpeningBalance { get; set; }
            public decimal ClosingBalance { get; set; }
            public List<LedgerLineDto> Lines { get; set; } = new();
        }

        public class TrialBalanceRowDto
        {
            public int AccountId { get; set; }
            public string AccountCode { get; set; } = string.Empty;
            public string AccountName { get; set; } = string.Empty;
            public AccountType Type { get; set; }
            public decimal TotalDebit { get; set; }
            public decimal TotalCredit { get; set; }
            public decimal Balance { get; set; }
        }

        public class ProfitLossDto
        {
            public List<TrialBalanceRowDto> Revenues { get; set; } = new();
            public List<TrialBalanceRowDto> Expenses { get; set; } = new();
            public decimal TotalRevenue { get; set; }
            public decimal TotalExpense { get; set; }
            public decimal NetProfit { get; set; }
        }

        public class BalanceSheetDto
        {
            public List<TrialBalanceRowDto> Assets { get; set; } = new();
            public List<TrialBalanceRowDto> Liabilities { get; set; } = new();
            public List<TrialBalanceRowDto> Equity { get; set; } = new();
            public decimal TotalAssets { get; set; }
            public decimal TotalLiabilities { get; set; }
            public decimal TotalEquity { get; set; }
            public decimal NetProfitToDate { get; set; }
        }

        public class JournalRowDto
        {
            public int JournalEntryId { get; set; }
            public int Number { get; set; }
            public DateTime EntryDate { get; set; }
            public string? Description { get; set; }
            public JournalEntryRefType RefType { get; set; }
            public List<JournalLineDto> Lines { get; set; } = new();
        }

        public class JournalLineDto
        {
            public string AccountCode { get; set; } = string.Empty;
            public string AccountName { get; set; } = string.Empty;
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public string? Description { get; set; }
        }

        // GET: api/financialreport/journal?from=&to=
        // دفتر روزنامه: همه اسناد به ترتیب تاریخ
        [HttpGet("journal")]
        public async Task<IActionResult> GetJournal([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var query = _context.JournalEntries
                .Include(e => e.Lines)!.ThenInclude(l => l.Account)
                .AsQueryable();

            if (from.HasValue) query = query.Where(e => e.EntryDate >= from.Value);
            if (to.HasValue) query = query.Where(e => e.EntryDate <= to.Value);

            var entries = await query.OrderBy(e => e.EntryDate).ThenBy(e => e.Number).ToListAsync();

            var result = entries.Select(e => new JournalRowDto
            {
                JournalEntryId = e.Id,
                Number = e.Number,
                EntryDate = e.EntryDate,
                Description = e.Description,
                RefType = e.RefType,
                Lines = e.Lines!.Select(l => new JournalLineDto
                {
                    AccountCode = l.Account?.Code ?? "",
                    AccountName = l.Account?.Name ?? "",
                    Debit = l.Debit,
                    Credit = l.Credit,
                    Description = l.Description
                }).ToList()
            }).ToList();

            return Ok(result);
        }

        // GET: api/financialreport/ledger/{accountId}?from=&to=
        // دفتر معین یک حساب خاص با مانده در حرکت
        [HttpGet("ledger/{accountId}")]
        public async Task<IActionResult> GetAccountLedger(int accountId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null) return NotFound();

            var openingQuery = _context.JournalEntryLines
                .Where(l => l.AccountId == accountId);
            if (from.HasValue) openingQuery = openingQuery.Where(l => l.JournalEntry!.EntryDate < from.Value);
            else openingQuery = openingQuery.Where(l => false);

            var opening = await openingQuery.SumAsync(l => l.Debit - l.Credit);

            var lineQuery = _context.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == accountId);
            if (from.HasValue) lineQuery = lineQuery.Where(l => l.JournalEntry!.EntryDate >= from.Value);
            if (to.HasValue) lineQuery = lineQuery.Where(l => l.JournalEntry!.EntryDate <= to.Value);

            var lines = await lineQuery
                .OrderBy(l => l.JournalEntry!.EntryDate)
                .ThenBy(l => l.JournalEntry!.Number)
                .ToListAsync();

            decimal running = opening;
            var lineDtos = new List<LedgerLineDto>();
            foreach (var l in lines)
            {
                running += l.Debit - l.Credit;
                lineDtos.Add(new LedgerLineDto
                {
                    JournalEntryId = l.JournalEntryId,
                    Number = l.JournalEntry!.Number,
                    EntryDate = l.JournalEntry!.EntryDate,
                    Description = l.Description ?? l.JournalEntry!.Description,
                    Debit = l.Debit,
                    Credit = l.Credit,
                    RunningBalance = running
                });
            }

            return Ok(new AccountLedgerDto
            {
                AccountId = account.Id,
                AccountCode = account.Code,
                AccountName = account.Name,
                OpeningBalance = opening,
                ClosingBalance = running,
                Lines = lineDtos
            });
        }

        // GET: api/financialreport/trialbalance?from=&to=
        // تراز آزمایشی: جمع بدهکار/بستانکار و مانده هر حساب
        [HttpGet("trialbalance")]
        public async Task<IActionResult> GetTrialBalance([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var rows = await BuildTrialBalance(from, to);
            return Ok(rows.OrderBy(r => r.AccountCode));
        }

        // GET: api/financialreport/profitloss?from=&to=
        // صورت سود و زیان
        [HttpGet("profitloss")]
        public async Task<IActionResult> GetProfitLoss([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var rows = await BuildTrialBalance(from, to);

            var revenues = rows.Where(r => r.Type == AccountType.Revenue).OrderBy(r => r.AccountCode).ToList();
            var expenses = rows.Where(r => r.Type == AccountType.Expense).OrderBy(r => r.AccountCode).ToList();

            // درآمد: بستانکار - بدهکار (مانده طبیعی بستانکار) => Balance منفی یعنی بستانکار
            var totalRevenue = revenues.Sum(r => -r.Balance);
            var totalExpense = expenses.Sum(r => r.Balance);

            return Ok(new ProfitLossDto
            {
                Revenues = revenues,
                Expenses = expenses,
                TotalRevenue = totalRevenue,
                TotalExpense = totalExpense,
                NetProfit = totalRevenue - totalExpense
            });
        }

        // GET: api/financialreport/balancesheet?asOf=
        // ترازنامه (تجمعی از ابتدا تا تاریخ مشخص)
        [HttpGet("balancesheet")]
        public async Task<IActionResult> GetBalanceSheet([FromQuery] DateTime? asOf)
        {
            var rows = await BuildTrialBalance(null, asOf);

            var assets = rows.Where(r => r.Type == AccountType.Asset).OrderBy(r => r.AccountCode).ToList();
            var liabilities = rows.Where(r => r.Type == AccountType.Liability).OrderBy(r => r.AccountCode).ToList();
            var equity = rows.Where(r => r.Type == AccountType.Equity).OrderBy(r => r.AccountCode).ToList();
            var revenues = rows.Where(r => r.Type == AccountType.Revenue).ToList();
            var expenses = rows.Where(r => r.Type == AccountType.Expense).ToList();

            var totalAssets = assets.Sum(r => r.Balance);
            var totalLiabilities = liabilities.Sum(r => -r.Balance);
            var totalEquity = equity.Sum(r => -r.Balance);
            var netProfit = revenues.Sum(r => -r.Balance) - expenses.Sum(r => r.Balance);

            return Ok(new BalanceSheetDto
            {
                Assets = assets,
                Liabilities = liabilities,
                Equity = equity,
                TotalAssets = totalAssets,
                TotalLiabilities = totalLiabilities,
                TotalEquity = totalEquity,
                NetProfitToDate = netProfit
            });
        }

        public class VatReportDto
        {
            public decimal InputVat { get; set; }   // مالیات خرید (قابل کسر)
            public decimal OutputVat { get; set; }  // مالیات فروش (قابل پرداخت)
            public decimal NetVat { get; set; }     // مثبت = پرداختنی به سازمان مالیاتی، منفی = قابل استرداد
        }

        // GET: api/financialreport/vat-report?from=&to=
        [HttpGet("vat-report")]
        public async Task<IActionResult> GetVatReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var purchaseQuery = _context.PurchaseInvoices.AsQueryable();
            if (from.HasValue) purchaseQuery = purchaseQuery.Where(p => p.CreatedAt >= from.Value);
            if (to.HasValue) purchaseQuery = purchaseQuery.Where(p => p.CreatedAt <= to.Value);
            var inputVat = await purchaseQuery.SumAsync(p => p.VatAmount);

            var saleQuery = _context.SaleInvoices.AsQueryable();
            if (from.HasValue) saleQuery = saleQuery.Where(s => s.CreatedAt >= from.Value);
            if (to.HasValue) saleQuery = saleQuery.Where(s => s.CreatedAt <= to.Value);
            var outputVat = await saleQuery.SumAsync(s => s.VatAmount);

            return Ok(new VatReportDto
            {
                InputVat = inputVat,
                OutputVat = outputVat,
                NetVat = outputVat - inputVat
            });
        }

        private async Task<List<TrialBalanceRowDto>> BuildTrialBalance(DateTime? from, DateTime? to)
        {
            var lineQuery = _context.JournalEntryLines
                .Include(l => l.Account)
                .Include(l => l.JournalEntry)
                .AsQueryable();

            if (from.HasValue) lineQuery = lineQuery.Where(l => l.JournalEntry!.EntryDate >= from.Value);
            if (to.HasValue) lineQuery = lineQuery.Where(l => l.JournalEntry!.EntryDate <= to.Value);

            var lines = await lineQuery.ToListAsync();

            var grouped = lines
                .Where(l => l.Account != null)
                .GroupBy(l => l.Account!)
                .Select(g => new TrialBalanceRowDto
                {
                    AccountId = g.Key.Id,
                    AccountCode = g.Key.Code,
                    AccountName = g.Key.Name,
                    Type = g.Key.Type,
                    TotalDebit = g.Sum(l => l.Debit),
                    TotalCredit = g.Sum(l => l.Credit),
                    Balance = g.Sum(l => l.Debit) - g.Sum(l => l.Credit)
                })
                .ToList();

            return grouped;
        }
    }
}
