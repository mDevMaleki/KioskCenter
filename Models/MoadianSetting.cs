using System.ComponentModel.DataAnnotations;

namespace KioskCenter.Models
{
    // تنظیمات اتصال به سامانه مودیان (سازمان امور مالیاتی)
    // اتصال فقط در صورتی فعال می‌شود که کاربر صریحاً IsEnabled را تیک بزند
    public class MoadianSetting
    {
        [Key]
        public int Id { get; set; }

        // کاربر باید این گزینه را صریحاً فعال کند تا ارسال خودکار فاکتورها انجام شود
        public bool IsEnabled { get; set; } = false;

        [MaxLength(4000)]
        public string? PublicKeyPem { get; set; }

        [MaxLength(4000)]
        public string? PrivateKeyPem { get; set; }

        // شناسه کلید سازمانی (Organization Key Id / شناسه یکتای حافظه مالیاتی)
        [MaxLength(100)]
        public string? OrgKeyId { get; set; }

        // شناسه کاربری ثبت‌شده نزد سامانه مودیان (معمولاً کد اقتصادی یا شناسه پایانه فروش)
        [MaxLength(100)]
        public string? Username { get; set; }

        [MaxLength(200)]
        public string BaseUrl { get; set; } = "https://tp.tax.gov.ir";

        public DateTime? LastUpdatedAt { get; set; }
    }
}
