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
    public class BudgetController : ControllerBase
    {
        private readonly CoffeeShopContext _context;

        public BudgetController(CoffeeShopContext context)
        {
            _context = context;
        }

        // GET: api/budget
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTime? periodStart, [FromQuery] DateTime? periodEnd)
        {
            var query = _context.Budgets.Include(b => b.Account).AsQueryable();

            if (periodStart.HasValue) query = query.Where(b => b.PeriodStart >= periodStart.Value);
            if (periodEnd.HasValue) query = query.Where(b => b.PeriodEnd <= periodEnd.Value);

            var items = await query
                .OrderBy(b => b.PeriodStart)
                .Select(b => new
                {
                    b.Id,
                    b.AccountId,
                    AccountCode = b.Account != null ? b.Account.Code : "",
                    AccountName = b.Account != null ? b.Account.Name : "",
                    b.PeriodStart,
                    b.PeriodEnd,
                    b.BudgetedAmount,
                    b.Note
                })
                .ToListAsync();

            return Ok(items);
        }

        // POST: api/budget
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BudgetDto dto)
        {
            if (dto.AccountId <= 0 || dto.BudgetedAmount <= 0 || dto.PeriodStart > dto.PeriodEnd)
                return BadRequest(new { success = false, message = "حساب، مبلغ بودجه و بازه زمانی معتبر الزامی است" });

            var account = await _context.Accounts.FindAsync(dto.AccountId);
            if (account == null) return BadRequest(new { success = false, message = "حساب یافت نشد" });

            var budget = new Budget
            {
                AccountId = dto.AccountId,
                PeriodStart = dto.PeriodStart,
                PeriodEnd = dto.PeriodEnd,
                BudgetedAmount = dto.BudgetedAmount,
                Note = dto.Note,
                CreatedAt = DateTime.Now
            };

            _context.Budgets.Add(budget);
            await _context.SaveChangesAsync();
            return Ok(budget);
        }

        // DELETE: api/budget/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var budget = await _context.Budgets.FindAsync(id);
            if (budget == null) return NotFound();

            _context.Budgets.Remove(budget);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // GET: api/budget/vs-actual?periodStart=&periodEnd=
        [HttpGet("vs-actual")]
        public async Task<IActionResult> GetBudgetVsActual([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
        {
            var budgets = await _context.Budgets
                .Include(b => b.Account)
                .Where(b => b.PeriodStart >= periodStart && b.PeriodEnd <= periodEnd)
                .ToListAsync();

            var result = new List<object>();

            foreach (var budget in budgets)
            {
                var actual = await _context.JournalEntryLines
                    .Where(l => l.AccountId == budget.AccountId
                        && l.JournalEntry!.EntryDate >= budget.PeriodStart
                        && l.JournalEntry!.EntryDate <= budget.PeriodEnd)
                    .SumAsync(l => l.Debit - l.Credit);

                var actualAmount = budget.Account?.Type == AccountType.Expense ? actual : -actual;

                result.Add(new
                {
                    budget.Id,
                    budget.AccountId,
                    AccountCode = budget.Account?.Code,
                    AccountName = budget.Account?.Name,
                    budget.PeriodStart,
                    budget.PeriodEnd,
                    budget.BudgetedAmount,
                    ActualAmount = actualAmount,
                    Variance = budget.BudgetedAmount - actualAmount,
                    VariancePercent = budget.BudgetedAmount != 0 ? Math.Round((actualAmount / budget.BudgetedAmount) * 100, 1) : 0
                });
            }

            return Ok(result);
        }
    }

    public class BudgetDto
    {
        public int AccountId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal BudgetedAmount { get; set; }
        public string? Note { get; set; }
    }
}
