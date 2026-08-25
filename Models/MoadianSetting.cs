using System.ComponentModel.DataAnnotations;

namespace KioskCenter.Models
{
    // تنظیمات اتصال به سامانه مودیان (سازمان امور مالیاتی) - بر اساس SDK رسمی TaxCollectData.Library
    // اتصال فقط در صورتی فعال می‌شود که کاربر صریحاً IsEnabled را تیک بزند
    public class MoadianSetting
    {
        [Key]
        public int Id { get; set; }

        // کاربر باید این گزینه را صریحاً فعال کند تا ارسال خودکار فاکتورها انجام شود
        public bool IsEnabled { get; set; } = false;

        // شناسه یکتای حافظه مالیاتی (Memory Id) - مثال: A11216
        [MaxLength(20)]
        public string? MemoryId { get; set; }

        // شماره اقتصادی فروشنده (tins) - جدا از Memory Id
        [MaxLength(20)]
        public string? SellerEconomicCode { get; set; }

        // آدرس API سامانه مودیان
        [MaxLength(200)]
        public string ApiUrl { get; set; } = "https://tp.tax.gov.ir/requestsmanager";

        // کلید خصوصی امضا به فرمت PKCS#8 PEM (محتوای فایل private_key.pem)
        [MaxLength(4000)]
        public string? PrivateKeyPem { get; set; }

        // گواهی امضای دیجیتال به فرمت Base64 (محتوای فایل certificate.cer/crt)
        [MaxLength(4000)]
        public string? CertificatePem { get; set; }

        public DateTime? LastUpdatedAt { get; set; }
    }
}
