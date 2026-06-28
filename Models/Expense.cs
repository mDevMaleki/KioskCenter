using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    // ثبت هزینه‌ها (مثل اجاره، آب و برق، حقوق و ...)
    public class Expense
    {
        [Key]
        public int Id { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // حساب هزینه (معین) که بدهکار می‌شود
        public int AccountId { get; set; }

        [ForeignKey("AccountId")]
        public virtual Account? Account { get; set; }

        // صندوق/بانکی که از آن پرداخت شده (بستانکار می‌شود)
        public int CashAccountId { get; set; }

        [ForeignKey("CashAccountId")]
        public virtual CashAccount? CashAccount { get; set; }

        // سند حسابداری مرتبط
        public int JournalEntryId { get; set; }

        [ForeignKey("JournalEntryId")]
        [JsonIgnore]
        public virtual JournalEntry? JournalEntry { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
