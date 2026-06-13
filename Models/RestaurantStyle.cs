using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KioskCenter.Models
{
    public class RestaurantStyle
    {
        [Key]
        public int Id { get; set; }

        // اطلاعات رستوران
        [MaxLength(200)]
        public string RestaurantName { get; set; } = "کافی شاپ آغادون";

        [MaxLength(500)]
        public string LogoUrl { get; set; } = "/images/logo.png";

        // رنگ‌های اصلی
        [MaxLength(50)]
        public string PrimaryColor { get; set; } = "#5C2E15";

        [MaxLength(50)]
        public string SecondaryColor { get; set; } = "#d4b896";

        [MaxLength(50)]
        public string AccentColor { get; set; } = "#ff6b35";

        [MaxLength(200)]
        public string BackgroundColor { get; set; } = "linear-gradient(135deg, #d4b896 0%, #c9a882 100%)";

        [MaxLength(50)]
        public string TextColor { get; set; } = "#ffffff";

        [MaxLength(50)]
        public string TextSecondaryColor { get; set; } = "rgba(255,255,255,0.8)";

        // رنگ‌های دکمه‌ها
        [MaxLength(50)]
        public string ButtonColor { get; set; } = "#28a745";

        [MaxLength(50)]
        public string ButtonHoverColor { get; set; } = "#1e7e34";

        [MaxLength(50)]
        public string ButtonTextColor { get; set; } = "#ffffff";

        // رنگ‌های کارت محصولات
        [MaxLength(50)]
        public string ProductCardBgColor { get; set; } = "#5C2E15";

        [MaxLength(50)]
        public string ProductCardHoverColor { get; set; } = "#6B3D1F";

        [MaxLength(20)]
        public string ProductCardBorderRadius { get; set; } = "24px";

        // رنگ‌های سایدبار
        [MaxLength(200)]
        public string SidebarBgColor { get; set; } = "linear-gradient(180deg, #060468 0%, #2326b6 100%)";

        [MaxLength(50)]
        public string SidebarHeaderBgColor { get; set; } = "rgba(0,0,0,0.2)";

        [MaxLength(50)]
        public string SidebarItemBgColor { get; set; } = "rgba(255,255,255,0.1)";

        [MaxLength(50)]
        public string SidebarItemHoverColor { get; set; } = "rgba(255,255,255,0.2)";

        // رنگ‌های دسته‌بندی
        [MaxLength(50)]
        public string CategoryBtnBgColor { get; set; } = "white";

        [MaxLength(50)]
        public string CategoryBtnActiveBgColor { get; set; } = "#6b4423";

        [MaxLength(50)]
        public string CategoryBtnTextColor { get; set; } = "#6b4423";

        [MaxLength(50)]
        public string CategoryBtnActiveTextColor { get; set; } = "white";

        // رنگ‌های نوع سفارش
        [MaxLength(50)]
        public string OrderTypeCardBgColor { get; set; } = "#0084fb";

        [MaxLength(50)]
        public string OrderTypeCardHoverColor { get; set; } = "#0094ff";

        [MaxLength(50)]
        public string OrderTypeCardTextColor { get; set; } = "white";

        // فونت‌ها
        [MaxLength(200)]
        public string FontFamily { get; set; } = "YekanBakh, Tahoma, sans-serif";

        [MaxLength(500)]
        public string CustomFontUrl { get; set; } = "";

        [MaxLength(100)]
        public string FontName { get; set; } = "YekanBakh";

        [MaxLength(10)]
        public string FontWeight { get; set; } = "500";

        [MaxLength(20)]
        public string FontStyle { get; set; } = "normal";

        [MaxLength(10)]
        public string FontSizeBase { get; set; } = "14px";

        [MaxLength(10)]
        public string FontSizeTitle { get; set; } = "24px";

        // بک‌گراندها
        [MaxLength(500)]
        public string BackgroundImage { get; set; } = "";

        [MaxLength(50)]
        public string BackgroundOverlay { get; set; } = "rgba(0,0,0,0.3)";

        // فوتر و اطلاعات
        [MaxLength(500)]
        public string FooterText { get; set; } = "";

        [MaxLength(50)]
        public string FooterBgColor { get; set; } = "rgba(0,0,0,0.8)";

        [MaxLength(50)]
        public string FooterTextColor { get; set; } = "#ffffff";

        [MaxLength(500)]
        public string Address { get; set; } = "";

        [MaxLength(50)]
        public string Phone { get; set; } = "";

        [MaxLength(200)]
        public string Instagram { get; set; } = "";

        [MaxLength(200)]
        public string WorkingHours { get; set; } = "";

        // حاشیه‌ها و فاصله‌ها
        [MaxLength(20)]
        public string CardBorderRadius { get; set; } = "20px";

        [MaxLength(20)]
        public string ButtonBorderRadius { get; set; } = "8px";

        [MaxLength(20)]
        public string SpacingBase { get; set; } = "16px";

        // انیمیشن‌ها
        public bool EnableAnimations { get; set; } = true;

        [MaxLength(10)]
        public string AnimationDuration { get; set; } = "0.3s";

        // وضعیت
        public bool IsActive { get; set; } = true;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}