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
    public class ExpenseController : ControllerBase
    {
        private readonly CoffeeShopContext _context;
        private readonly JournalPostingService _postingService;

        public ExpenseController(CoffeeShopContext context, JournalPostingService postingService)
        {
            _context = context;
            _postingService = postingService;
        }

        // GET: api/expense - فهرست هزینه‌ها
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _context.Expenses
                .Include(e => e.Account)
                .Include(e => e.CashAccount)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(e => e.Date >= from.Value);

            if (to.HasValue)
                query = query.Where(e => e.Date <= to.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new
                {
                    e.Id,
                    e.Date,
                    e.Description,
                    e.Amount,
                    e.AccountId,
                    AccountName = e.Account != null ? e.Account.Name : null,
                    e.CashAccountId,
                    CashAccountName = e.CashAccount != null ? e.CashAccount.Name : null,
                    e.JournalEntryId
                })
                .ToListAsync();

            var totalAmount = await query.SumAsync(e => e.Amount);

            return Ok(new { total, totalAmount, page, pageSize, items });
        }

        // POST: api/expense - ثبت هزینه جدید
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ExpenseDto dto)
        {
            if (dto.Amount <= 0)
                return BadRequest(new { success = false, message = "مبلغ هزینه باید بزرگ‌تر از صفر باشد" });

            if (dto.AccountId <= 0)
                return BadRequest(new { success = false, message = "انتخاب حساب هزینه الزامی است" });

            if (dto.CashAccountId <= 0)
                return BadRequest(new { success = false, message = "انتخاب صندوق/بانک پرداخت‌کننده الزامی است" });

            var cashAccount = await _context.CashAccounts.FindAsync(dto.CashAccountId);
            if (cashAccount == null)
                return BadRequest(new { success = false, message = "صندوق/بانک مورد نظر یافت نشد" });

            try
            {
                var entryDate = dto.Date == default ? DateTime.Now : dto.Date;

                var entry = await _postingService.PostAsync(
                    entryDate,
                    dto.Description,
                    JournalEntryRefType.Expense,
                    null,
                    new List<JournalLineInput>
                    {
                        new JournalLineInput { AccountId = dto.AccountId, Debit = dto.Amount, Description = dto.Description },
                        new JournalLineInput { AccountId = cashAccount.AccountId, Credit = dto.Amount, CashAccountId = cashAccount.Id, Description = dto.Description }
                    });

                var expense = new Expense
                {
                    Date = entryDate,
                    Description = dto.Description,
                    Amount = dto.Amount,
                    AccountId = dto.AccountId,
                    CashAccountId = dto.CashAccountId,
                    JournalEntryId = entry.Id,
                    CreatedAt = DateTime.Now
                };

                _context.Expenses.Add(expense);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "هزینه با موفقیت ثبت شد", id = expense.Id });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // DELETE: api/expense/{id} - حذف هزینه (و برگشت سند حسابداری مرتبط)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null)
                return NotFound(new { success = false, message = "هزینه یافت نشد" });

            var journalEntryId = expense.JournalEntryId;

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();

            await _postingService.ReverseAsync(journalEntryId);

            return Ok(new { success = true, message = "هزینه با موفقیت حذف شد" });
        }
    }

    public class ExpenseDto
    {
        public DateTime Date { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public int AccountId { get; set; }
        public int CashAccountId { get; set; }
    }
}
