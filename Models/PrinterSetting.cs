using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace KioskCenter.Models
{
    public class PrinterSetting
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = ""; // مثلاً "آشپزخانه", "صندوق", "کیوسک"
        public string PrinterName { get; set; } = ""; // نام واقعی پرینتر در ویندوز
        public string PrinterType { get; set; } = "Receipt"; // Receipt, Kitchen, Label
        public bool IsActive { get; set; } = true;
        public int Priority { get; set; } = 0;
        public List<string> Categories { get; set; } = new(); // برای پرینتر آشپزخانه - کدام دسته‌ها
        public List<int> ProductIds { get; set; } = new(); // برای پرینتر آشپزخانه - کدام محصولات
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}