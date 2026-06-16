using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public enum JournalEntryRefType
    {
        Manual = 0,          // سند دستی
        PurchaseInvoice = 1, // فاکتور خرید
        SaleInvoice = 2,     // فاکتور فروش
        Payment = 3,         // سند پرداخت
        Receipt = 4,         // سند دریافت
        Expense = 5          // هزینه
    }

    public class JournalEntry
    {
        [Key]
        public int Id { get; set; }

        // شماره سند نمایشی
        public int Number { get; set; }

        public DateTime EntryDate { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string? Description { get; set; }

        public JournalEntryRefType RefType { get; set; } = JournalEntryRefType.Manual;

        public int? RefId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<JournalEntryLine>? Lines { get; set; }
    }

    public class JournalEntryLine
    {
        [Key]
        public int Id { get; set; }

        public int JournalEntryId { get; set; }

        public int AccountId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Debit { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Credit { get; set; } = 0;

        [MaxLength(500)]
        public string? Description { get; set; }

        // در صورتی که حساب طرف، یک حساب کنترل (دریافتنی/پرداختنی) باشد
        public int? PartyId { get; set; }

        // در صورتی که حساب طرف، یک حساب صندوق/بانک باشد
        public int? CashAccountId { get; set; }

        [ForeignKey("JournalEntryId")]
        [JsonIgnore]
        public virtual JournalEntry? JournalEntry { get; set; }

        [ForeignKey("AccountId")]
        public virtual Account? Account { get; set; }

        [ForeignKey("PartyId")]
        public virtual Party? Party { get; set; }

        [ForeignKey("CashAccountId")]
        public virtual CashAccount? CashAccount { get; set; }
    }
}
