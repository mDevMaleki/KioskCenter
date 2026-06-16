using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public enum CashAccountType
    {
        Cash = 1, // صندوق
        Bank = 2  // بانک
    }

    public class CashAccount
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public CashAccountType Type { get; set; } = CashAccountType.Cash;

        [MaxLength(50)]
        public string? AccountNumber { get; set; }

        // حساب معین مرتبط در کدینگ حسابداری
        public int AccountId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("AccountId")]
        [JsonIgnore]
        public virtual Account? Account { get; set; }
    }
}
