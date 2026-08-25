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

        // این پرینتر برای کدام منبع سفارش فعال است: Kiosk، Cashier یا Both (هر دو)
        // اگر برای صندوق پرینتر مخصوصی تعریف نشده باشد (هیچ Cashier/Both)، به‌صورت خودکار از پرینتر Kiosk استفاده می‌شود
        public string Source { get; set; } = "Both";
        public bool IsActive { get; set; } = true;
        public int Priority { get; set; } = 0;
        public List<string> Categories { get; set; } = new(); // برای پرینتر آشپزخانه - کدام دسته‌ها
        public List<int> ProductIds { get; set; } = new(); // برای پرینتر آشپزخانه - کدام محصولات
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}