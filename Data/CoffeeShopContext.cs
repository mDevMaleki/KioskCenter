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

            // ایندکس یکتا برای OrderTypeId
            modelBuilder.Entity<OrderTypeSettings>()
                .HasIndex(e => e.OrderTypeId)
                .IsUnique();
        

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
        }
    }
}