using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public class UnitOfMeasure
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        // اگر null باشد، خودش یک واحد پایه است
        public int? BaseUnitId { get; set; }

        // ضریب تبدیل: هر ۱ واحد از این واحد برابر است با چند واحد پایه
        [Column(TypeName = "decimal(18,6)")]
        public decimal ConversionFactor { get; set; } = 1;

        [ForeignKey("BaseUnitId")]
        [JsonIgnore]
        public virtual UnitOfMeasure? BaseUnit { get; set; }
    }
}
