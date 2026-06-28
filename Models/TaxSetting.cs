using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KioskCenter.Models
{
    public class TaxSetting
    {
        [Key]
        public int Id { get; set; }

        // درصد مالیات بر ارزش افزوده، مثلاً 9.00 یعنی 9 درصد
        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; } = 9;

        public bool IsEnabled { get; set; } = true;
    }
}
