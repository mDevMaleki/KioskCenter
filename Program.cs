using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using KioskCenter.Services;
using KioskCenter.Data;
using KioskCenter.Services.PardakhtNovinPos.PcPos;
using KioskCenter.Hubs;
using KioskCenter.Middleware;
using KioskCenter.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// اضافه کردن Swagger برای دات نت 9
builder.Services.AddOpenApi();

// تنظیم اتصال به SQL Server
builder.Services.AddDbContext<CoffeeShopContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<PNAPcPos>(provider =>
{
    var pos = new PNAPcPos();
    // می‌توانید تنظیمات اولیه را اینجا انجام دهید
    return pos;
});
// اضافه کنید
builder.Services.AddScoped<IPosManagerService, PosManagerService>();
builder.Services.Configure<ReceiptPrintingOptions>(builder.Configuration.GetSection("ReceiptPrinting"));
builder.Services.AddScoped<IReceiptPrinter, DefaultReceiptPrinter>();

// ثبت سرویس‌ها
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<PosParsianService>();
builder.Services.AddScoped<OnlinePayService>();

builder.Services.AddSingleton<HardwareService>();
builder.Services.AddSingleton<LicenseValidator>();
builder.Services.AddSingleton<LicenseManager>();
builder.Services.AddHttpClient<LicenseRefreshService>();


// ========== تنظیم CORS - فقط یک بار ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // برای حالت با Credentials (اگر نیاز شد)
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();
app.UseMiddleware<LicenseMiddleware>();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CoffeeShopContext>();
    db.Database.Migrate(); // دیتابیس را می‌سازد و همه Migration ها را اعمال می‌کند
}
// ========== ترتیب صحیح Middlewareها (خیلی مهم) ==========
// 1.先用 UseCors
app.UseCors("AllowAll");  // استفاده از policy اول

// 2. سپس UseWebSockets (اگه SignalR دارید)
app.UseWebSockets();

// 3. بعد UseHttpsRedirection
app.UseHttpsRedirection();

// 4. بعد UseStaticFiles
app.UseStaticFiles();

// 5. بعد UseRouting
app.UseRouting();

// 6. بعد UseAuthorization
app.UseAuthorization();

// 7. Finally MapHub و MapControllers
app.MapHub<PaymentHub>("/paymentHub");
app.MapControllers();

// Configure the HTTP request pipeline for Swagger
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Aghadon API v1");
    });
}

// ایجاد دیتابیس در صورت عدم وجود
using (var scope = app.Services.CreateScope())
{
    // --- اضافه کنید ---
    var hwService = scope.ServiceProvider.GetRequiredService<HardwareService>();
    string myHash = hwService.GetHardwareHash();
    Console.WriteLine("========================================");
    Console.WriteLine($"YOUR HARDWARE ID: {myHash}");
    Console.WriteLine("========================================");
    // -----------------

    var manager = scope.ServiceProvider.GetRequiredService<LicenseManager>();
    if (!await manager.ValidateOnStartup())
    {
        Console.WriteLine("Application blocked due to invalid license.");
        Environment.Exit(1);
    }

    var dbContext = scope.ServiceProvider.GetRequiredService<CoffeeShopContext>();
    dbContext.Database.EnsureCreated();
}







app.Run();