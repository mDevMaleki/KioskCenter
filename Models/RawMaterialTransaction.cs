using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public enum RawMaterialTransactionType
    {
        In = 1,
        Out = 2,
        Adjustment = 3
    }

    public class RawMaterialTransaction
    {
        [Key]
        public int Id { get; set; }

        public int RawMaterialId { get; set; }

        public RawMaterialTransactionType Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal StockAfter { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalPrice { get; set; }

        [MaxLength(200)]
        public string? PartyName { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("RawMaterialId")]
        [JsonIgnore]
        public virtual RawMaterial? RawMaterial { get; set; }
    }
}
