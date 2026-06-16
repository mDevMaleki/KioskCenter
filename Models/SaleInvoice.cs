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

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

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
