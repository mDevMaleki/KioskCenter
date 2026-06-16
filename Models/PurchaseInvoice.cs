using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public class PurchaseInvoice
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

        public virtual ICollection<PurchaseInvoiceItem>? Items { get; set; }
    }

    public class PurchaseInvoiceItem
    {
        [Key]
        public int Id { get; set; }

        public int PurchaseInvoiceId { get; set; }

        public int RawMaterialId { get; set; }

        public int UnitId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [ForeignKey("PurchaseInvoiceId")]
        [JsonIgnore]
        public virtual PurchaseInvoice? PurchaseInvoice { get; set; }

        [ForeignKey("RawMaterialId")]
        public virtual RawMaterial? RawMaterial { get; set; }

        [ForeignKey("UnitId")]
        public virtual UnitOfMeasure? Unit { get; set; }
    }
}
