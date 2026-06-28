using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public class FixedAsset
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchaseValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalvageValue { get; set; } = 0;

        public int UsefulLifeMonths { get; set; }

        // حساب دارایی (مثلاً اموال، ماشین‌آلات)
        public int AssetAccountId { get; set; }

        // حساب هزینه استهلاک
        public int DepreciationExpenseAccountId { get; set; }

        // حساب استهلاک انباشته (تماس‌گیرنده دارایی)
        public int AccumulatedDepreciationAccountId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AccumulatedDepreciation { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("AssetAccountId")]
        [JsonIgnore]
        public virtual Account? AssetAccount { get; set; }

        [ForeignKey("DepreciationExpenseAccountId")]
        [JsonIgnore]
        public virtual Account? DepreciationExpenseAccount { get; set; }

        [ForeignKey("AccumulatedDepreciationAccountId")]
        [JsonIgnore]
        public virtual Account? AccumulatedDepreciationAccount { get; set; }

        [JsonIgnore]
        public virtual ICollection<DepreciationRecord>? DepreciationRecords { get; set; }
    }

    public class DepreciationRecord
    {
        [Key]
        public int Id { get; set; }

        public int FixedAssetId { get; set; }

        public DateTime PeriodDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public int? JournalEntryId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("FixedAssetId")]
        [JsonIgnore]
        public virtual FixedAsset? FixedAsset { get; set; }
    }
}
