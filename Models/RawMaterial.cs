using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public class RawMaterial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public int UnitId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal StockQuantity { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinStockLevel { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UnitId")]
        public virtual UnitOfMeasure? Unit { get; set; }

        [JsonIgnore]
        public virtual ICollection<RawMaterialTransaction>? Transactions { get; set; }

        [JsonIgnore]
        public virtual ICollection<ProductRecipeItem>? RecipeItems { get; set; }
    }
}
