using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KioskCenter.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public string OrderType { get; set; } = "EatIn"; // EatIn | TakeAway

        public int OrderNumber { get; set; } // شماره سفارش

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string? PaymentStatus { get; set; } // "موفق", "ناموفق"

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } // مالیات

        [MaxLength(200)]
        public string? CustomerName { get; set; } // مانند "یعقوب پر"
        public int? TableNumber { get; set; }

        // روش پرداخت انتخاب شده برای این سفارش
        public int? PaymentMethodId { get; set; }

        // منبع ثبت سفارش: Kiosk (دستگاه خودسرویس مشتری) یا Cashier (صندوق فروشگاهی) - برای انتخاب پرینتر مناسب
        [MaxLength(20)]
        public string Source { get; set; } = "Kiosk";

        // سفارش پارک‌شده در صندوق فروشگاهی (هنوز تسویه نشده، کنار گذاشته شده برای ادامه بعدی)
        public bool IsParked { get; set; } = false;

        [MaxLength(100)]
        public string? ParkedLabel { get; set; } // مثلاً "میز ۳" یا نام مشتری

        [ForeignKey("PaymentMethodId")]
        public virtual PaymentMethod? PaymentMethod { get; set; }

        // رابطه یک به چند با آیتم‌های سفارش
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
