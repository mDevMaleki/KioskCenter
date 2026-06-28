using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public enum ChequeDirection
    {
        Received = 1, // دریافتی
        Paid = 2      // پرداختی
    }

    public enum ChequeStatus
    {
        InHand = 1,     // نزد ما / صادر شده
        Deposited = 2,  // به بانک سپرده شده (فقط دریافتی)
        Cleared = 3,    // وصول / پاس شده
        Bounced = 4,    // برگشتی
        Returned = 5    // مرجوع به طرف حساب
    }

    public class Cheque
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Number { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? BankName { get; set; }

        public DateTime IssueDate { get; set; } = DateTime.Now;

        public DateTime DueDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public ChequeDirection Direction { get; set; }

        public ChequeStatus Status { get; set; } = ChequeStatus.InHand;

        public int PartyId { get; set; }

        // صندوق/بانکی که چک نهایتاً به آن وصل می‌شود (هنگام وصول/پرداخت)
        public int? CashAccountId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("PartyId")]
        [JsonIgnore]
        public virtual Party? Party { get; set; }

        [ForeignKey("CashAccountId")]
        [JsonIgnore]
        public virtual CashAccount? CashAccount { get; set; }
    }
}
