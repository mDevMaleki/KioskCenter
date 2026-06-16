using KioskCenter.Data;
using KioskCenter.Models;
using Microsoft.EntityFrameworkCore;

namespace KioskCenter.Services
{
    public class JournalLineInput
    {
        public int AccountId { get; set; }
        public decimal Debit { get; set; } = 0;
        public decimal Credit { get; set; } = 0;
        public string? Description { get; set; }
        public int? PartyId { get; set; }
        public int? CashAccountId { get; set; }
    }

    // سرویس ثبت اسناد حسابداری (دوطرفه) و به‌روزرسانی مانده حساب‌ها
    public class JournalPostingService
    {
        private readonly CoffeeShopContext _context;

        public JournalPostingService(CoffeeShopContext context)
        {
            _context = context;
        }

        public async Task<JournalEntry> PostAsync(
            DateTime entryDate,
            string? description,
            JournalEntryRefType refType,
            int? refId,
            List<JournalLineInput> lines)
        {
            if (lines == null || lines.Count < 2)
                throw new InvalidOperationException("سند حسابداری باید حداقل دو ردیف داشته باشد");

            var totalDebit = lines.Sum(l => l.Debit);
            var totalCredit = lines.Sum(l => l.Credit);

            if (totalDebit != totalCredit)
                throw new InvalidOperationException("سند حسابداری باید موازنه باشد (جمع بدهکار = جمع بستانکار)");

            if (totalDebit <= 0)
                throw new InvalidOperationException("جمع سند نمی‌تواند صفر باشد");

            foreach (var line in lines)
            {
                var account = await _context.Accounts.FindAsync(line.AccountId);
                if (account == null)
                    throw new InvalidOperationException("حساب مورد نظر یافت نشد");

                if (account.IsGroup)
                    throw new InvalidOperationException($"حساب '{account.Name}' از نوع گروه است و قابل ثبت سند مستقیم نیست");
            }

            var lastNumber = await _context.JournalEntries
                .OrderByDescending(e => e.Number)
                .Select(e => e.Number)
                .FirstOrDefaultAsync();

            var entry = new JournalEntry
            {
                Number = lastNumber + 1,
                EntryDate = entryDate,
                Description = description,
                RefType = refType,
                RefId = refId,
                CreatedAt = DateTime.Now,
                Lines = new List<JournalEntryLine>()
            };

            foreach (var line in lines)
            {
                entry.Lines.Add(new JournalEntryLine
                {
                    AccountId = line.AccountId,
                    Debit = line.Debit,
                    Credit = line.Credit,
                    Description = line.Description,
                    PartyId = line.PartyId,
                    CashAccountId = line.CashAccountId
                });

                var account = await _context.Accounts.FindAsync(line.AccountId);
                account!.Balance += line.Debit - line.Credit;

                if (line.CashAccountId.HasValue)
                {
                    var cashAccount = await _context.CashAccounts.FindAsync(line.CashAccountId.Value);
                    if (cashAccount == null)
                        throw new InvalidOperationException("صندوق/بانک مورد نظر یافت نشد");

                    cashAccount.Balance += line.Debit - line.Credit;
                }
            }

            _context.JournalEntries.Add(entry);
            await _context.SaveChangesAsync();

            return entry;
        }

        // برگشت یک سند حسابداری (اصلاح مانده حساب‌ها)
        public async Task ReverseAsync(int journalEntryId)
        {
            var entry = await _context.JournalEntries
                .Include(e => e.Lines)
                .FirstOrDefaultAsync(e => e.Id == journalEntryId);

            if (entry == null)
                throw new InvalidOperationException("سند حسابداری یافت نشد");

            foreach (var line in entry.Lines ?? new List<JournalEntryLine>())
            {
                var account = await _context.Accounts.FindAsync(line.AccountId);
                if (account != null)
                    account.Balance -= line.Debit - line.Credit;

                if (line.CashAccountId.HasValue)
                {
                    var cashAccount = await _context.CashAccounts.FindAsync(line.CashAccountId.Value);
                    if (cashAccount != null)
                        cashAccount.Balance -= line.Debit - line.Credit;
                }
            }

            _context.JournalEntryLines.RemoveRange(entry.Lines ?? new List<JournalEntryLine>());
            _context.JournalEntries.Remove(entry);

            await _context.SaveChangesAsync();
        }
    }
}
