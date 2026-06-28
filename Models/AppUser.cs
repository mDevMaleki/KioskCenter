using System.ComponentModel.DataAnnotations;

namespace KioskCenter.Models
{
    public class AppUser
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        // اگر true باشد، کاربر به همه‌ی بخش‌ها دسترسی کامل دارد
        public bool IsSuperAdmin { get; set; }

        public bool IsActive { get; set; } = true;

        // لیست کلیدهای بخش‌های پنل ادمین که کاربر به آن دسترسی دارد، با کاما جدا شده
        // مثال: "products,categories,inventory"
        [MaxLength(500)]
        public string Permissions { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
