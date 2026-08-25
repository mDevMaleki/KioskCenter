namespace KioskCenter.Authorization
{
    // کلیدهای بخش‌های پنل ادمین که می‌توان دسترسی به هرکدام را جداگانه به کاربران داد
    public static class AdminSections
    {
        public static readonly string[] All =
        {
            "products",
            "categories",
            "order-type",
            "inventory",
            "printers",
            "pos-devices",
            "payment-methods",
            "style",
            "reports",
            "users",
            "purchase-sale",
            "accounting",
            "cashier"
        };
    }
}
