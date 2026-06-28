using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public enum AccountType
    {
        Asset = 1,     // دارایی
        Liability = 2, // بدهی
        Equity = 3,    // حقوق صاحبان سرمایه
        Revenue = 4,   // درآمد
        Expense = 5    // هزینه
    }

    public class Account
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public AccountType Type { get; set; }

        public int? ParentId { get; set; }

        // حساب گروه (کل) قابل ثبت سند مستقیم نیست، فقط برای دسته‌بندی است
        public bool IsGroup { get; set; } = false;

        public bool IsActive { get; set; } = true;

        // مانده حساب به‌صورت (بدهکار - بستانکار)
        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0;

        [ForeignKey("ParentId")]
        [JsonIgnore]
        public virtual Account? Parent { get; set; }
    }
}
