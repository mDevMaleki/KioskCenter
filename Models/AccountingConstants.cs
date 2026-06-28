namespace KioskCenter.Models
{
    // شناسه‌های ثابت حساب‌های پایه کدینگ حسابداری (در CoffeeShopContext seed شده‌اند)
    public static class AccountingConstants
    {
        public const int AssetsGroup = 1;          // 1000 - دارایی‌ها
        public const int CashAndBankGroup = 2;     // 1100 - صندوق و بانک
        public const int AccountsReceivable = 3;   // 1200 - حساب‌های دریافتنی
        public const int RawMaterialInventory = 4; // 1300 - موجودی مواد اولیه

        public const int LiabilitiesGroup = 5;     // 2000 - بدهی‌ها
        public const int AccountsPayable = 6;      // 2100 - حساب‌های پرداختنی

        public const int EquityGroup = 7;          // 3000 - حقوق صاحبان سرمایه
        public const int Capital = 8;              // 3100 - سرمایه

        public const int RevenueGroup = 9;         // 4000 - درآمدها
        public const int SalesRevenue = 10;        // 4100 - فروش محصولات

        public const int ExpensesGroup = 11;       // 5000 - هزینه‌ها
        public const int GeneralExpense = 12;      // 5100 - هزینه‌های عمومی

        public const int CashSubAccount = 13;      // 1101 - صندوق
        public const int BankSubAccount = 14;      // 1102 - بانک

        // صندوق/بانک پیش‌فرض (CashAccount.Id)
        public const int DefaultCashAccount = 1;
        public const int DefaultBankAccount = 2;

        public const int VatReceivable = 24; // 1220 - مالیات بر ارزش افزوده خرید (دریافتنی از سازمان مالیاتی)
        public const int VatPayable = 25;    // 2300 - مالیات بر ارزش افزوده فروش (پرداختنی به سازمان مالیاتی)
    }
}
