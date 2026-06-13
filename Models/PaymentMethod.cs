using System.ComponentModel.DataAnnotations;

namespace KioskCenter.Models
{
    public class PaymentMethod
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = ""; // "Cash", "Pos", "Online"
        public string Title { get; set; } = ""; // "پرداخت نقدی", "کارت خوان", "پرداخت آنلاین"
        public string Icon { get; set; } = ""; // "💵", "💳", "📱"
        public string Description { get; set; } = ""; // توضیحات روش پرداخت
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public bool RequiresPosDevice { get; set; } = false; // آیا به دستگاه POS نیاز دارد
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}