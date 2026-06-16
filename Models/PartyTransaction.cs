using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public enum PartyTransactionType
    {
        PurchaseInvoice = 1, // فاکتور خرید - بدهی ما به طرف حساب افزایش می‌یابد
        SaleInvoice = 2,     // فاکتور فروش - بدهی طرف حساب به ما افزایش می‌یابد
        Payment = 3,         // پرداخت ما به طرف حساب - بدهی ما کاهش می‌یابد
        Receipt = 4          // دریافت از طرف حساب - بدهی او به ما کاهش می‌یابد
    }

    public class PartyTransaction
    {
        [Key]
        public int Id { get; set; }

        public int PartyId { get; set; }

        public PartyTransactionType Type { get; set; }

        // مقدار اثر بر مانده حساب (با علامت) - مثبت یعنی بدهکاری طرف‌حساب به ما زیاد می‌شود
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfter { get; set; }

        public int? RefId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("PartyId")]
        [JsonIgnore]
        public virtual Party? Party { get; set; }
    }
}
