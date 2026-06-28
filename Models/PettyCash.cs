using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public enum PettyCashTransactionType
    {
        Replenish = 1, // شارژ تنخواه
        Spend = 2      // مصرف تنخواه
    }

    public class PettyCashFund
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Custodian { get; set; } // مسئول تنخواه

        // صندوق/بانک منبع شارژ
        public int SourceCashAccountId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("SourceCashAccountId")]
        [JsonIgnore]
        public virtual CashAccount? SourceCashAccount { get; set; }

        [JsonIgnore]
        public virtual ICollection<PettyCashTransaction>? Transactions { get; set; }
    }

    public class PettyCashTransaction
    {
        [Key]
        public int Id { get; set; }

        public int PettyCashFundId { get; set; }

        public PettyCashTransactionType Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // فقط برای مصرف: حساب هزینه مرتبط
        public int? ExpenseAccountId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        public int? JournalEntryId { get; set; }

        [ForeignKey("PettyCashFundId")]
        [JsonIgnore]
        public virtual PettyCashFund? PettyCashFund { get; set; }

        [ForeignKey("ExpenseAccountId")]
        [JsonIgnore]
        public virtual Account? ExpenseAccount { get; set; }
    }

    public class Budget
    {
        [Key]
        public int Id { get; set; }

        public int AccountId { get; set; }

        // دوره بودجه به‌صورت اول ماه (مثلاً 2026-07-01) برای بودجه ماهانه
        public DateTime PeriodStart { get; set; }

        public DateTime PeriodEnd { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BudgetedAmount { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("AccountId")]
        [JsonIgnore]
        public virtual Account? Account { get; set; }
    }
}
