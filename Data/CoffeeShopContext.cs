using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;
using KioskCenter.Models;

namespace KioskCenter.Data
{
    public class CoffeeShopContext : DbContext
    {
        public CoffeeShopContext(DbContextOptions<CoffeeShopContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<PrinterSetting> PrinterSettings { get; set; }
        public DbSet<PosDevice> PosDevices { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }

        public DbSet<OrderTypeSettings> OrderTypeSettings { get; set; } // اضافه کنید

        public DbSet<RestaurantStyle> RestaurantStyles { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<AppUser> Users { get; set; }
        public DbSet<RawMaterial> RawMaterials { get; set; }
        public DbSet<RawMaterialTransaction> RawMaterialTransactions { get; set; }
        public DbSet<ProductRecipeItem> ProductRecipeItems { get; set; }
        public DbSet<UnitOfMeasure> UnitsOfMeasure { get; set; }
        public DbSet<Party> Parties { get; set; }
        public DbSet<PartyTransaction> PartyTransactions { get; set; }
        public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }
        public DbSet<SaleInvoice> SaleInvoices { get; set; }
        public DbSet<SaleInvoiceItem> SaleInvoiceItems { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<CashAccount> CashAccounts { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<JournalEntryLine> JournalEntryLines { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Cheque> Cheques { get; set; }
        public DbSet<FixedAsset> FixedAssets { get; set; }
        public DbSet<DepreciationRecord> DepreciationRecords { get; set; }
        public DbSet<PettyCashFund> PettyCashFunds { get; set; }
        public DbSet<PettyCashTransaction> PettyCashTransactions { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<FiscalYear> FiscalYears { get; set; }
        public DbSet<TaxSetting> TaxSettings { get; set; }
        public DbSet<MoadianSetting> MoadianSettings { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // تنظیمات Product
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.SecondPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.StockQuantity)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.MinStockLevel)
                .HasPrecision(18, 2);

            // رابطه Product با InventoryTransaction
            modelBuilder.Entity<InventoryTransaction>()
                .HasOne(t => t.Product)
                .WithMany(p => p.InventoryTransactions)
                .HasForeignKey(t => t.ProductId);

            // ایندکس یکتا برای OrderTypeId
            modelBuilder.Entity<OrderTypeSettings>()
                .HasIndex(e => e.OrderTypeId)
                .IsUnique();

            // تنظیمات RawMaterial
            modelBuilder.Entity<RawMaterial>()
                .Property(r => r.StockQuantity)
                .HasPrecision(18, 2);

            modelBuilder.Entity<RawMaterial>()
                .Property(r => r.MinStockLevel)
                .HasPrecision(18, 2);

            // رابطه RawMaterial با RawMaterialTransaction
            modelBuilder.Entity<RawMaterialTransaction>()
                .HasOne(t => t.RawMaterial)
                .WithMany(r => r.Transactions)
                .HasForeignKey(t => t.RawMaterialId);

            // رابطه فرمول محصول (Product <-> RawMaterial)
            modelBuilder.Entity<ProductRecipeItem>()
                .HasOne(ri => ri.Product)
                .WithMany()
                .HasForeignKey(ri => ri.ProductId);

            modelBuilder.Entity<ProductRecipeItem>()
                .HasOne(ri => ri.RawMaterial)
                .WithMany(r => r.RecipeItems)
                .HasForeignKey(ri => ri.RawMaterialId);

            // تنظیمات Party
            modelBuilder.Entity<Party>()
                .Property(p => p.Balance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PartyTransaction>()
                .HasOne(t => t.Party)
                .WithMany(p => p.Transactions)
                .HasForeignKey(t => t.PartyId);

            // واحد پایه (خودارجاع) - برای جلوگیری از چرخه cascade
            modelBuilder.Entity<UnitOfMeasure>()
                .HasOne(u => u.BaseUnit)
                .WithMany()
                .HasForeignKey(u => u.BaseUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // RawMaterial -> Unit
            modelBuilder.Entity<RawMaterial>()
                .HasOne(r => r.Unit)
                .WithMany()
                .HasForeignKey(r => r.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // فاکتور خرید
            modelBuilder.Entity<PurchaseInvoice>()
                .HasOne(i => i.Party)
                .WithMany()
                .HasForeignKey(i => i.PartyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseInvoiceItem>()
                .HasOne(i => i.PurchaseInvoice)
                .WithMany(i => i.Items)
                .HasForeignKey(i => i.PurchaseInvoiceId);

            modelBuilder.Entity<PurchaseInvoiceItem>()
                .HasOne(i => i.RawMaterial)
                .WithMany()
                .HasForeignKey(i => i.RawMaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseInvoiceItem>()
                .HasOne(i => i.Unit)
                .WithMany()
                .HasForeignKey(i => i.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // فاکتور فروش
            modelBuilder.Entity<SaleInvoice>()
                .HasOne(i => i.Party)
                .WithMany()
                .HasForeignKey(i => i.PartyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleInvoiceItem>()
                .HasOne(i => i.SaleInvoice)
                .WithMany(i => i.Items)
                .HasForeignKey(i => i.SaleInvoiceId);

            modelBuilder.Entity<SaleInvoiceItem>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // کدینگ حساب‌ها (خودارجاع به والد)
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Parent)
                .WithMany()
                .HasForeignKey(a => a.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Account>()
                .Property(a => a.Balance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Account>()
                .HasIndex(a => a.Code)
                .IsUnique();

            // صندوق و بانک
            modelBuilder.Entity<CashAccount>()
                .HasOne(c => c.Account)
                .WithMany()
                .HasForeignKey(c => c.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashAccount>()
                .Property(c => c.Balance)
                .HasPrecision(18, 2);

            // اسناد حسابداری
            modelBuilder.Entity<JournalEntryLine>()
                .HasOne(l => l.JournalEntry)
                .WithMany(e => e.Lines)
                .HasForeignKey(l => l.JournalEntryId);

            modelBuilder.Entity<JournalEntryLine>()
                .HasOne(l => l.Account)
                .WithMany()
                .HasForeignKey(l => l.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<JournalEntryLine>()
                .HasOne(l => l.Party)
                .WithMany()
                .HasForeignKey(l => l.PartyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<JournalEntryLine>()
                .HasOne(l => l.CashAccount)
                .WithMany()
                .HasForeignKey(l => l.CashAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<JournalEntryLine>()
                .Property(l => l.Debit)
                .HasPrecision(18, 2);

            modelBuilder.Entity<JournalEntryLine>()
                .Property(l => l.Credit)
                .HasPrecision(18, 2);

            // کدینگ پیش‌فرض حساب‌ها
            modelBuilder.Entity<Account>().HasData(
                new Account { Id = 1, Code = "1000", Name = "دارایی‌ها", Type = AccountType.Asset, ParentId = null, IsGroup = true },
                new Account { Id = 2, Code = "1100", Name = "صندوق و بانک", Type = AccountType.Asset, ParentId = 1, IsGroup = true },
                new Account { Id = 3, Code = "1200", Name = "حساب‌های دریافتنی", Type = AccountType.Asset, ParentId = 1, IsGroup = false },
                new Account { Id = 4, Code = "1300", Name = "موجودی مواد اولیه", Type = AccountType.Asset, ParentId = 1, IsGroup = false },
                new Account { Id = 5, Code = "2000", Name = "بدهی‌ها", Type = AccountType.Liability, ParentId = null, IsGroup = true },
                new Account { Id = 6, Code = "2100", Name = "حساب‌های پرداختنی", Type = AccountType.Liability, ParentId = 5, IsGroup = false },
                new Account { Id = 7, Code = "3000", Name = "حقوق صاحبان سرمایه", Type = AccountType.Equity, ParentId = null, IsGroup = true },
                new Account { Id = 8, Code = "3100", Name = "سرمایه", Type = AccountType.Equity, ParentId = 7, IsGroup = false },
                new Account { Id = 9, Code = "4000", Name = "درآمدها", Type = AccountType.Revenue, ParentId = null, IsGroup = true },
                new Account { Id = 10, Code = "4100", Name = "فروش محصولات", Type = AccountType.Revenue, ParentId = 9, IsGroup = false },
                new Account { Id = 11, Code = "5000", Name = "هزینه‌ها", Type = AccountType.Expense, ParentId = null, IsGroup = true },
                new Account { Id = 12, Code = "5100", Name = "هزینه‌های عمومی", Type = AccountType.Expense, ParentId = 11, IsGroup = false },
                new Account { Id = 13, Code = "1101", Name = "صندوق", Type = AccountType.Asset, ParentId = 2, IsGroup = false },
                new Account { Id = 14, Code = "1102", Name = "بانک", Type = AccountType.Asset, ParentId = 2, IsGroup = false },
                new Account { Id = 15, Code = "1210", Name = "اسناد دریافتنی (چک)", Type = AccountType.Asset, ParentId = 1, IsGroup = false },
                new Account { Id = 16, Code = "2200", Name = "اسناد پرداختنی (چک)", Type = AccountType.Liability, ParentId = 5, IsGroup = false },
                new Account { Id = 21, Code = "1400", Name = "دارایی‌های ثابت", Type = AccountType.Asset, ParentId = 1, IsGroup = false },
                new Account { Id = 22, Code = "1410", Name = "استهلاک انباشته دارایی ثابت", Type = AccountType.Asset, ParentId = 1, IsGroup = false },
                new Account { Id = 23, Code = "1500", Name = "تنخواه‌گردان", Type = AccountType.Asset, ParentId = 1, IsGroup = false },
                new Account { Id = 24, Code = "1220", Name = "مالیات بر ارزش افزوده خرید", Type = AccountType.Asset, ParentId = 1, IsGroup = false },
                new Account { Id = 25, Code = "2300", Name = "مالیات بر ارزش افزوده فروش", Type = AccountType.Liability, ParentId = 5, IsGroup = false }
            );

            // صندوق و بانک پیش‌فرض
            modelBuilder.Entity<CashAccount>().HasData(
                new CashAccount { Id = 1, Name = "صندوق", Type = CashAccountType.Cash, AccountId = 13, CreatedAt = new DateTime(2026, 1, 1) },
                new CashAccount { Id = 2, Name = "بانک", Type = CashAccountType.Bank, AccountId = 14, CreatedAt = new DateTime(2026, 1, 1) }
            );

            modelBuilder.Entity<TaxSetting>().HasData(
                new TaxSetting { Id = 1, VatRate = 9, IsEnabled = true }
            );

            // واحدهای پیش‌فرض
            modelBuilder.Entity<UnitOfMeasure>().HasData(
                new UnitOfMeasure { Id = 1, Name = "گرم", BaseUnitId = null, ConversionFactor = 1 },
                new UnitOfMeasure { Id = 2, Name = "کیلوگرم", BaseUnitId = 1, ConversionFactor = 1000 },
                new UnitOfMeasure { Id = 3, Name = "میلی‌لیتر", BaseUnitId = null, ConversionFactor = 1 },
                new UnitOfMeasure { Id = 4, Name = "لیتر", BaseUnitId = 3, ConversionFactor = 1000 },
                new UnitOfMeasure { Id = 5, Name = "عدد", BaseUnitId = null, ConversionFactor = 1 }
            );

        // جلوگیری از تداخل cascade برای حساب‌های مرتبط با دارایی ثابت
        modelBuilder.Entity<FixedAsset>()
            .HasOne(f => f.AssetAccount)
            .WithMany()
            .HasForeignKey(f => f.AssetAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FixedAsset>()
            .HasOne(f => f.DepreciationExpenseAccount)
            .WithMany()
            .HasForeignKey(f => f.DepreciationExpenseAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FixedAsset>()
            .HasOne(f => f.AccumulatedDepreciationAccount)
            .WithMany()
            .HasForeignKey(f => f.AccumulatedDepreciationAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PettyCashTransaction>()
            .HasOne(t => t.ExpenseAccount)
            .WithMany()
            .HasForeignKey(t => t.ExpenseAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Budget>()
            .HasOne(b => b.Account)
            .WithMany()
            .HasForeignKey(b => b.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Cheque>()
            .HasOne(c => c.Party)
            .WithMany()
            .HasForeignKey(c => c.PartyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Cheque>()
            .HasOne(c => c.CashAccount)
            .WithMany()
            .HasForeignKey(c => c.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PettyCashFund>()
            .HasOne(p => p.SourceCashAccount)
            .WithMany()
            .HasForeignKey(p => p.SourceCashAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // رابطه Category با Product
        modelBuilder.Entity<Category>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId);

            // رابطه Order با OrderItem
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId);

            // رابطه Product با OrderItem
            modelBuilder.Entity<Product>()
                .HasMany(p => p.OrderItems)
                .WithOne(oi => oi.Product)
                .HasForeignKey(oi => oi.ProductId);

            // اضافه کردن data seeding
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "کافی یار", Description = "اسپرسو، آمریکانو، لته، قهوه و ..." },
                new Category { Id = 2, Name = "کافی سرد", Description = "آیس اسپرسو، آیس آمریکانو و ..." },
                new Category { Id = 3, Name = "چای دمی", Description = "چای سیاه، چای زعفرانی و ..." },
                new Category { Id = 4, Name = "نوشیدنی گرم", Description = "نسکافه، هات چاکلت، شیر نوتالا و ..." },
                new Category { Id = 5, Name = "طعم دهنده", Description = "طبیعی، مصنوعی و ..." },
                new Category { Id = 6, Name = "بیکری", Description = "چیز کیک، دبل چاکلت و ..." }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "اسپرسو دوبل", Price = 130000, CategoryId = 1 },
                new Product { Id = 2, Name = "اسپرسو تک", Price = 90000, CategoryId = 1 },
                new Product { Id = 3, Name = "آمریکانو", Price = 120000, CategoryId = 1 },
                new Product { Id = 4, Name = "لاته", Price = 140000, CategoryId = 1 },
                new Product { Id = 5, Name = "چیز کیک", Price = 85000, CategoryId = 6 },
                new Product { Id = 6, Name = "دبل چاکلت", Price = 95000, CategoryId = 6 }
            );

            // کاربر ادمین پیش‌فرض - نام کاربری: admin / رمز عبور: admin123
            modelBuilder.Entity<AppUser>().HasData(
                new AppUser
                {
                    Id = 1,
                    Username = "admin",
                    FullName = "مدیر سیستم",
                    PasswordHash = "AAAAAAAAAAAAAAAAAAAAAA==.UQx0m4jTwaXcLRcbF4Ue97OtKlJsoS/o1HhHds97js0=",
                    IsSuperAdmin = true,
                    IsActive = true,
                    Permissions = "",
                    CreatedAt = new DateTime(2026, 1, 1)
                }
            );
        }
    }
}