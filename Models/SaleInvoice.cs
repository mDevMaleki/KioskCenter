using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public class SaleInvoice
    {
        [Key]
        public int Id { get; set; }

        public int PartyId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // وضعیت ارسال به سامانه مودیان (اختیاری - فقط در صورت فعال‌سازی توسط کاربر)
        [MaxLength(50)]
        public string? MoadianTaxId { get; set; }

        public bool MoadianSent { get; set; } = false;

        public DateTime? MoadianSentAt { get; set; }

        [MaxLength(100)]
        public string? MoadianReferenceNumber { get; set; }

        [MaxLength(1000)]
        public string? MoadianError { get; set; }

        [ForeignKey("PartyId")]
        [JsonIgnore]
        public virtual Party? Party { get; set; }

        public virtual ICollection<SaleInvoiceItem>? Items { get; set; }
    }

    public class SaleInvoiceItem
    {
        [Key]
        public int Id { get; set; }

        public int SaleInvoiceId { get; set; }

        public int ProductId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [ForeignKey("SaleInvoiceId")]
        [JsonIgnore]
        public virtual SaleInvoice? SaleInvoice { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}
