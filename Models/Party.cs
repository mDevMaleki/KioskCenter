using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public enum PartyType
    {
        Supplier = 1,
        Customer = 2,
        Both = 3
    }

    public class Party
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public PartyType Type { get; set; } = PartyType.Both;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        // مانده حساب: مثبت = طرف حساب به ما بدهکار است، منفی = ما به او بدهکار هستیم (بستانکار)
        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonIgnore]
        public virtual ICollection<PartyTransaction>? Transactions { get; set; }
    }
}
