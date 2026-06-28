using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using KioskCenter.Services;
using KioskCenter.Data;
using KioskCenter.Hubs;
using KioskCenter.Middleware;
using KioskCenter.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService();

// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// اضافه کردن Swagger برای دات نت 9
builder.Services.AddOpenApi();

// تنظیم اتصال به SQL Server
builder.Services.AddDbContext<CoffeeShopContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// اضافه کنید
builder.Services.AddScoped<IPosManagerService, PosManagerService>();
builder.Services.Configure<ReceiptPrintingOptions>(builder.Configuration.GetSection("ReceiptPrinting"));
builder.Services.AddScoped<IReceiptPrinter, DefaultReceiptPrinter>();
builder.Services.AddScoped<IPosService, PosService>();
builder.Services.AddScoped<IPardakhtNovinService,PardakhtNovinService>();

// ثبت سرویس‌ها
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<PosParsianService>();
builder.Services.AddScoped<OnlinePayService>();
builder.Services.AddScoped<KioskCenter.Services.JournalPostingService>();
builder.Services.AddScoped<KioskCenter.Services.MoadianService>();

builder.Services.AddSingleton<HardwareService>();
builder.Services.AddSingleton<LicenseValidator>();
builder.Services.AddSingleton<LicenseManager>();
builder.Services.AddHttpClient<LicenseRefreshService>();

// سرویس‌های احراز هویت کاربران پنل ادمین
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<JwtService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "kiosk-center-default-dev-secret-key-change-me-1234567890";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "KioskCenter",
            ValidateAudience = true,
            ValidAudience = "KioskCenter",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();


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

// 4. بعد UseStaticFiles
app.UseStaticFiles();

// 5. بعد UseRouting
app.UseRouting();

// 6. بعد UseAuthentication و UseAuthorization
app.UseAuthentication();
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
        Console.WriteLine("License invalid - running in limited mode (license activation only).");
    }

    var dbContext = scope.ServiceProvider.GetRequiredService<CoffeeShopContext>();
    dbContext.Database.EnsureCreated();
}







app.Run();