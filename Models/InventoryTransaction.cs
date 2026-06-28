using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public enum InventoryTransactionType
    {
        In = 1,
        Out = 2,
        Adjustment = 3
    }

    public class InventoryTransaction
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        public InventoryTransactionType Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal StockAfter { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ProductId")]
        [JsonIgnore]
        public virtual Product? Product { get; set; }
    }
}
