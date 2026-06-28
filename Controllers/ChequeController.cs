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
    public class ChequeController : ControllerBase
    {
        private readonly CoffeeShopContext _context;
        private readonly JournalPostingService _postingService;

        // کدهای حساب کنترل چک (در seed داده شده)
        private const string ReceivableChequeCode = "1210"; // اسناد دریافتنی
        private const string PayableChequeCode = "2200";    // اسناد پرداختنی
        private const string PayableAccountCode = "2100";   // حساب‌های پرداختنی (طرف حساب)
        private const string ReceivableAccountCode = "1200"; // حساب‌های دریافتنی (طرف حساب)

        public ChequeController(CoffeeShopContext context, JournalPostingService postingService)
        {
            _context = context;
            _postingService = postingService;
        }

        private async Task<Account> GetControlAccount(string code)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == code);
            if (account == null)
                throw new InvalidOperationException($"حساب کنترل با کد {code} یافت نشد");
            return account;
        }

        // GET: api/cheque?direction=&status=&partyId=
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ChequeDirection? direction, [FromQuery] ChequeStatus? status, [FromQuery] int? partyId)
        {
            var query = _context.Cheques.Include(c => c.Party).Include(c => c.CashAccount).AsQueryable();

            if (direction.HasValue) query = query.Where(c => c.Direction == direction.Value);
            if (status.HasValue) query = query.Where(c => c.Status == status.Value);
            if (partyId.HasValue) query = query.Where(c => c.PartyId == partyId.Value);

            var items = await query
                .OrderBy(c => c.DueDate)
                .Select(c => new
                {
                    c.Id,
                    c.Number,
                    c.BankName,
                    c.IssueDate,
                    c.DueDate,
                    c.Amount,
                    c.Direction,
                    c.Status,
                    c.PartyId,
                    PartyName = c.Party != null ? c.Party.Name : null,
                    c.CashAccountId,
                    CashAccountName = c.CashAccount != null ? c.CashAccount.Name : null,
                    c.Description
                })
                .ToListAsync();

            return Ok(items);
        }

        // GET: api/cheque/due-soon?days=7
        [HttpGet("due-soon")]
        public async Task<IActionResult> GetDueSoon([FromQuery] int days = 7)
        {
            var cutoff = DateTime.Now.Date.AddDays(days);
            var items = await _context.Cheques
                .Include(c => c.Party)
                .Where(c => c.Status == ChequeStatus.InHand && c.DueDate <= cutoff)
                .OrderBy(c => c.DueDate)
                .Select(c => new
                {
                    c.Id,
                    c.Number,
                    c.DueDate,
                    c.Amount,
                    c.Direction,
                    PartyName = c.Party != null ? c.Party.Name : null
                })
                .ToListAsync();

            return Ok(items);
        }

        // POST: api/cheque - ثبت چک جدید (دریافتی یا پرداختی)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ChequeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Number) || dto.Amount <= 0 || dto.PartyId <= 0)
                return BadRequest(new { success = false, message = "شماره چک، مبلغ و طرف حساب الزامی است" });

            var party = await _context.Parties.FindAsync(dto.PartyId);
            if (party == null)
                return BadRequest(new { success = false, message = "طرف حساب یافت نشد" });

            var cheque = new Cheque
            {
                Number = dto.Number,
                BankName = dto.BankName,
                IssueDate = dto.IssueDate,
                DueDate = dto.DueDate,
                Amount = dto.Amount,
                Direction = dto.Direction,
                Status = ChequeStatus.InHand,
                PartyId = dto.PartyId,
                Description = dto.Description,
                CreatedAt = DateTime.Now
            };

            _context.Cheques.Add(cheque);
            await _context.SaveChangesAsync();

            // ثبت سند: چک دریافتی => بدهکار اسناد دریافتنی / بستانکار حساب‌های دریافتنی (طرف حساب)
            // چک پرداختی => بدهکار حساب‌های پرداختنی (طرف حساب) / بستانکار اسناد پرداختنی
            var lines = new List<JournalLineInput>();

            if (dto.Direction == ChequeDirection.Received)
            {
                var receivableCheque = await GetControlAccount(ReceivableChequeCode);
                var receivableAccount = await GetControlAccount(ReceivableAccountCode);

                lines.Add(new JournalLineInput { AccountId = receivableCheque.Id, Debit = dto.Amount, Description = $"دریافت چک شماره {dto.Number}", PartyId = dto.PartyId });
                lines.Add(new JournalLineInput { AccountId = receivableAccount.Id, Credit = dto.Amount, Description = $"دریافت چک شماره {dto.Number}", PartyId = dto.PartyId });

                party.Balance -= dto.Amount; // طرف حساب کمتر به ما بدهکار می‌شود
            }
            else
            {
                var payableAccount = await GetControlAccount(PayableAccountCode);
                var payableCheque = await GetControlAccount(PayableChequeCode);

                lines.Add(new JournalLineInput { AccountId = payableAccount.Id, Debit = dto.Amount, Description = $"صدور چک شماره {dto.Number}", PartyId = dto.PartyId });
                lines.Add(new JournalLineInput { AccountId = payableCheque.Id, Credit = dto.Amount, Description = $"صدور چک شماره {dto.Number}", PartyId = dto.PartyId });

                party.Balance += dto.Amount; // ما کمتر بستانکار او هستیم (نزد او بدهکار کمتر)
            }

            await _postingService.PostAsync(cheque.IssueDate, $"ثبت چک {dto.Number}",
                dto.Direction == ChequeDirection.Received ? JournalEntryRefType.ChequeReceived : JournalEntryRefType.ChequePaid,
                cheque.Id, lines);

            await _context.SaveChangesAsync();

            return Ok(cheque);
        }

        // POST: api/cheque/{id}/deposit - سپردن چک دریافتی به بانک
        [HttpPost("{id}/deposit")]
        public async Task<IActionResult> Deposit(int id, [FromBody] ChequeActionDto dto)
        {
            var cheque = await _context.Cheques.FindAsync(id);
            if (cheque == null) return NotFound();
            if (cheque.Direction != ChequeDirection.Received || cheque.Status != ChequeStatus.InHand)
                return BadRequest(new { success = false, message = "این عملیات فقط برای چک دریافتی نزد ما مجاز است" });

            var cashAccount = await _context.CashAccounts.FindAsync(dto.CashAccountId);
            if (cashAccount == null) return BadRequest(new { success = false, message = "صندوق/بانک یافت نشد" });

            cheque.Status = ChequeStatus.Deposited;
            cheque.CashAccountId = dto.CashAccountId;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "چک به بانک سپرده شد", cheque });
        }

        // POST: api/cheque/{id}/clear - وصول/پاس‌شدن چک (نهایی)
        [HttpPost("{id}/clear")]
        public async Task<IActionResult> Clear(int id, [FromBody] ChequeActionDto dto)
        {
            var cheque = await _context.Cheques.FindAsync(id);
            if (cheque == null) return NotFound();
            if (cheque.Status == ChequeStatus.Cleared)
                return BadRequest(new { success = false, message = "این چک قبلاً وصول شده است" });

            var cashAccountId = dto.CashAccountId != 0 ? dto.CashAccountId : cheque.CashAccountId;
            if (cashAccountId == null || cashAccountId == 0)
                return BadRequest(new { success = false, message = "صندوق/بانک مقصد الزامی است" });

            var cashAccount = await _context.CashAccounts.FindAsync(cashAccountId.Value);
            if (cashAccount == null) return BadRequest(new { success = false, message = "صندوق/بانک یافت نشد" });

            var lines = new List<JournalLineInput>();

            if (cheque.Direction == ChequeDirection.Received)
            {
                var receivableCheque = await GetControlAccount(ReceivableChequeCode);
                lines.Add(new JournalLineInput { AccountId = cashAccount.AccountId, Debit = cheque.Amount, Description = $"وصول چک شماره {cheque.Number}", CashAccountId = cashAccount.Id });
                lines.Add(new JournalLineInput { AccountId = receivableCheque.Id, Credit = cheque.Amount, Description = $"وصول چک شماره {cheque.Number}", PartyId = cheque.PartyId });
            }
            else
            {
                var payableCheque = await GetControlAccount(PayableChequeCode);
                lines.Add(new JournalLineInput { AccountId = payableCheque.Id, Debit = cheque.Amount, Description = $"پرداخت چک شماره {cheque.Number}", PartyId = cheque.PartyId });
                lines.Add(new JournalLineInput { AccountId = cashAccount.AccountId, Credit = cheque.Amount, Description = $"پرداخت چک شماره {cheque.Number}", CashAccountId = cashAccount.Id });
            }

            await _postingService.PostAsync(DateTime.Now, $"وصول/پرداخت چک {cheque.Number}", JournalEntryRefType.ChequeStatusChange, cheque.Id, lines);

            cheque.Status = ChequeStatus.Cleared;
            cheque.CashAccountId = cashAccount.Id;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "چک با موفقیت وصول/پرداخت شد", cheque });
        }

        // POST: api/cheque/{id}/bounce - برگشت خوردن چک
        [HttpPost("{id}/bounce")]
        public async Task<IActionResult> Bounce(int id)
        {
            var cheque = await _context.Cheques.FindAsync(id);
            if (cheque == null) return NotFound();
            if (cheque.Status == ChequeStatus.Cleared)
                return BadRequest(new { success = false, message = "چک وصول‌شده قابل برگشت نیست" });

            cheque.Status = ChequeStatus.Bounced;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "چک به‌عنوان برگشتی ثبت شد", cheque });
        }

        // POST: api/cheque/{id}/return - مرجوع کردن چک به طرف حساب (ابطال سند ابتدایی)
        [HttpPost("{id}/return")]
        public async Task<IActionResult> Return(int id)
        {
            var cheque = await _context.Cheques.FindAsync(id);
            if (cheque == null) return NotFound();
            if (cheque.Status == ChequeStatus.Cleared)
                return BadRequest(new { success = false, message = "چک وصول‌شده قابل مرجوع کردن نیست" });

            var party = await _context.Parties.FindAsync(cheque.PartyId);

            var lines = new List<JournalLineInput>();

            if (cheque.Direction == ChequeDirection.Received)
            {
                var receivableCheque = await GetControlAccount(ReceivableChequeCode);
                var receivableAccount = await GetControlAccount(ReceivableAccountCode);
                lines.Add(new JournalLineInput { AccountId = receivableAccount.Id, Debit = cheque.Amount, Description = $"مرجوع چک شماره {cheque.Number}", PartyId = cheque.PartyId });
                lines.Add(new JournalLineInput { AccountId = receivableCheque.Id, Credit = cheque.Amount, Description = $"مرجوع چک شماره {cheque.Number}", PartyId = cheque.PartyId });
                if (party != null) party.Balance += cheque.Amount;
            }
            else
            {
                var payableAccount = await GetControlAccount(PayableAccountCode);
                var payableCheque = await GetControlAccount(PayableChequeCode);
                lines.Add(new JournalLineInput { AccountId = payableCheque.Id, Debit = cheque.Amount, Description = $"مرجوع چک شماره {cheque.Number}", PartyId = cheque.PartyId });
                lines.Add(new JournalLineInput { AccountId = payableAccount.Id, Credit = cheque.Amount, Description = $"مرجوع چک شماره {cheque.Number}", PartyId = cheque.PartyId });
                if (party != null) party.Balance -= cheque.Amount;
            }

            await _postingService.PostAsync(DateTime.Now, $"مرجوع چک {cheque.Number}", JournalEntryRefType.ChequeStatusChange, cheque.Id, lines);

            cheque.Status = ChequeStatus.Returned;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "چک مرجوع شد", cheque });
        }

        // DELETE: api/cheque/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cheque = await _context.Cheques.FindAsync(id);
            if (cheque == null) return NotFound();
            if (cheque.Status != ChequeStatus.InHand)
                return BadRequest(new { success = false, message = "فقط چک با وضعیت اولیه قابل حذف است؛ از عملیات «مرجوع» استفاده کنید" });

            return BadRequest(new { success = false, message = "برای حذف، ابتدا چک را «مرجوع» کنید تا سند اصلاحی ثبت شود" });
        }
    }

    public class ChequeDto
    {
        public string Number { get; set; } = string.Empty;
        public string? BankName { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public ChequeDirection Direction { get; set; }
        public int PartyId { get; set; }
        public string? Description { get; set; }
    }

    public class ChequeActionDto
    {
        public int CashAccountId { get; set; }
    }
}
