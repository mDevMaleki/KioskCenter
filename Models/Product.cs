using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SecondPrice { get; set; }
        public int CategoryId { get; set; }

        public int State { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Description { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
        [JsonIgnore]
        // اضافه کردن این خاصیت برای رابطه با OrderItem
        public virtual ICollection<OrderItem>? OrderItems { get; set; }
    }
}