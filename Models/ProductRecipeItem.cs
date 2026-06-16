using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public class ProductRecipeItem
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        public int RawMaterialId { get; set; }

        // مقدار مصرفی این ماده اولیه برای تولید یک واحد از محصول
        [Column(TypeName = "decimal(18,4)")]
        public decimal Quantity { get; set; }

        [ForeignKey("ProductId")]
        [JsonIgnore]
        public virtual Product? Product { get; set; }

        [ForeignKey("RawMaterialId")]
        public virtual RawMaterial? RawMaterial { get; set; }
    }
}
