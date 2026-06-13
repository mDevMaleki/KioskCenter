using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public class OrderTypeSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string OrderTypeId { get; set; } = string.Empty; // "EatIn" or "TakeAway"

        [Required]
        public string SettingsJson { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string UpdatedBy { get; set; } = "Admin";
    }

    public class OrderTypeSettingDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PersianName { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool Active { get; set; } = true;
        public decimal PriceFactor { get; set; } = 1;
        public List<string> VisibleCategoryIds { get; set; } = new();
        public List<CategoryPriceRule> CategoryPriceRules { get; set; } = new();
    }

    public class CategoryPriceRule
    {
        public string CategoryId { get; set; } = string.Empty;
        public decimal Factor { get; set; } = 1;
    }
}