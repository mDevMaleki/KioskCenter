using System.Diagnostics;
using System.Drawing.Printing;
using System.Text;
using KioskCenter.Helper;
using KioskCenter.Data;
using KioskCenter.Interfaces;
using KioskCenter.Models;
using Microsoft.EntityFrameworkCore;
using OpenHtmlToPdf;

namespace KioskCenter.Services
{
    public sealed class DefaultReceiptPrinter : IReceiptPrinter
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<DefaultReceiptPrinter> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public DefaultReceiptPrinter(
            IWebHostEnvironment env,
            ILogger<DefaultReceiptPrinter> logger,
            IServiceScopeFactory scopeFactory)
        {
            _env = env;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        // ========== پیاده‌سازی اینترفیس ==========

        public bool TryPrint(Order order, int orderNumber, int invoiceNumber)
        {
            try
            {
                // خواندن تنظیمات پرینترها از دیتابیس در زمان چاپ
                var allPrinters = GetActivePrintersFromDb().Result;

                if (allPrinters == null || !allPrinters.Any())
                {
                    _logger.LogWarning("No active printers found in database");
                    return false;
                }

                _logger.LogInformation($"Found {allPrinters.Count} active printers in database");

                // جدا کردن پرینترها بر اساس نوع
                var receiptPrinters = ResolveReceiptPrintersForSource(allPrinters, order.Source);
                var kitchenPrinters = allPrinters.Where(p => p.PrinterType == "Kitchen" && p.IsActive).ToList();
                var labelPrinters = allPrinters.Where(p => p.PrinterType == "Label" && p.IsActive).ToList();

                bool allSuccess = true;

                // 1. چاپ فیش مشتری (روی همه پرینترهای فیش)
                var receiptPdf = GenerateReceiptPdf(order, orderNumber, invoiceNumber);
                if (receiptPdf != null && receiptPdf.Length > 0)
                {
                    foreach (var printer in receiptPrinters)
                    {
                        var success = PrintToSpecificPrinter(receiptPdf, printer.PrinterName);
                        if (success)
                        {
                            _logger.LogInformation("✅ Receipt printed on '{PrinterName}' (ID: {Id}, Name: {Name})",
                                printer.PrinterName, printer.Id, printer.Name);
                        }
                        else
                        {
                            _logger.LogWarning("❌ Failed to print receipt on '{PrinterName}'", printer.PrinterName);
                            allSuccess = false;
                        }
                    }
                }

                // 2. چاپ در آشپزخانه (بر اساس تنظیمات هر پرینتر)
                var orderItems = order.OrderItems.ToList();

                foreach (var printer in kitchenPrinters)
                {
                    var itemsForThisPrinter = FilterItemsForPrinter(orderItems, printer);

                    if (itemsForThisPrinter.Any())
                    {
                        var kitchenPdf = GenerateKitchenReceiptPdf(order, orderNumber, invoiceNumber, printer, itemsForThisPrinter);
                        if (kitchenPdf != null && kitchenPdf.Length > 0)
                        {
                            var success = PrintToSpecificPrinter(kitchenPdf, printer.PrinterName);
                            if (success)
                            {
                                _logger.LogInformation("✅ Kitchen receipt printed on '{PrinterName}' (Name: {Name}) with {ItemCount} items",
                                    printer.PrinterName, printer.Name, itemsForThisPrinter.Count);
                            }
                            else
                            {
                                _logger.LogWarning("❌ Failed to print kitchen receipt on '{PrinterName}'", printer.PrinterName);
                                allSuccess = false;
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No matching items for printer '{PrinterName}' (Name: {Name})",
                            printer.PrinterName, printer.Name);
                    }
                }

                // 3. چاپ برچسب‌ها (در صورت وجود)
                foreach (var printer in labelPrinters)
                {
                    var labelPdf = GenerateLabelPdf(order, orderNumber, printer);
                    if (labelPdf != null && labelPdf.Length > 0)
                    {
                        var success = PrintToSpecificPrinter(labelPdf, printer.PrinterName);
                        if (success)
                        {
                            _logger.LogInformation("✅ Label printed on '{PrinterName}' (Name: {Name})",
                                printer.PrinterName, printer.Name);
                        }
                        else
                        {
                            _logger.LogWarning("❌ Failed to print label on '{PrinterName}'", printer.PrinterName);
                            allSuccess = false;
                        }
                    }
                }

                return allSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TryPrint for order {OrderNumber}", orderNumber);
                return false;
            }
        }

        public bool PrintReport(byte[] pdfBytes)
        {
            try
            {
                var printers = GetActivePrintersFromDb().Result;
                var receiptPrinters = printers.Where(p => p.PrinterType == "Receipt" && p.IsActive).ToList();

                bool allSuccess = true;
                foreach (var printer in receiptPrinters)
                {
                    var success = PrintToSpecificPrinter(pdfBytes, printer.PrinterName);
                    if (!success) allSuccess = false;
                }

                return allSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PrintReport");
                return false;
            }
        }

        public bool TestPrint(string printerName)
        {
            try
            {
                var testPdf = CreateTestPdf();
                return PrintToSpecificPrinter(testPdf, printerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test print to {PrinterName}", printerName);
                return false;
            }
        }

        public bool PrintToSpecificPrinter(byte[] pdfBytes, string printerName)
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");
                File.WriteAllBytes(tempPath, pdfBytes);

                var process = new Process();
                process.StartInfo.FileName = Path.Combine(AppContext.BaseDirectory, "SumatraPDF.exe"); // مسیر فایل اجرایی Sumatra
                                                               // دستور چاپ دقیق برای سوماترا
                process.StartInfo.Arguments = $"-print-to \"{printerName}\" \"{tempPath}\"";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;

                process.Start();
                process.WaitForExit(10000); // منتظر بمانید تا چاپ شود

                // پاک کردن فایل
                File.Delete(tempPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to print via Sumatra to {PrinterName}", printerName);
                return false;
            }
        }


        // ========== متدهای خصوصی ==========

        private async Task<List<PrinterSetting>> GetActivePrintersFromDb()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CoffeeShopContext>();

            var printers = await context.PrinterSettings
                .Where(p => p.IsActive)
                .OrderBy(p => p.Priority)
                .ToListAsync();

            _logger.LogInformation($"Loaded {printers.Count} active printers from database at {DateTime.Now:HH:mm:ss}");

            foreach (var p in printers)
            {
                _logger.LogInformation($"  - Printer: ID={p.Id}, Name={p.Name}, Type={p.PrinterType}, " +
                    $"PrinterName={p.PrinterName}, Categories={string.Join(",", p.Categories)}");
            }

            return printers;
        }

        // انتخاب پرینترهای فیش متناسب با منبع سفارش (کیوسک یا صندوق)
        // اگر برای منبع موردنظر هیچ پرینتر اختصاصی/مشترک تعریف نشده باشد، به‌صورت خودکار از پرینتر کیوسک استفاده می‌شود
        private List<PrinterSetting> ResolveReceiptPrintersForSource(List<PrinterSetting> allPrinters, string? orderSource)
        {
            var source = string.IsNullOrWhiteSpace(orderSource) ? "Kiosk" : orderSource;

            var receiptPrinters = allPrinters
                .Where(p => p.PrinterType == "Receipt" && p.IsActive)
                .ToList();

            var matching = receiptPrinters
                .Where(p => string.IsNullOrWhiteSpace(p.Source) || p.Source == "Both" || p.Source == source)
                .ToList();

            if (matching.Any())
                return matching;

            // نبود پرینتر مخصوص صندوق: استفاده از پرینتر کیوسک به‌عنوان جایگزین
            if (source == "Cashier")
            {
                _logger.LogInformation("No dedicated Cashier receipt printer found - falling back to Kiosk printer(s)");
                return receiptPrinters.Where(p => p.Source == "Kiosk").ToList();
            }

            return matching;
        }

        private List<OrderItem> FilterItemsForPrinter(List<OrderItem> items, PrinterSetting printer)
        {
            var filteredItems = items.ToList();

            // فیلتر بر اساس Categories (نام دسته‌بندی)
            if (printer.Categories != null && printer.Categories.Any())
            {
                filteredItems = filteredItems
                    .Where(i => i.Product?.Category != null &&
                        printer.Categories.Contains(i.Product.Category.Name))
                    .ToList();
            }

            // فیلتر بر اساس ProductIds
            if (printer.ProductIds != null && printer.ProductIds.Any())
            {
                var productIdMatches = filteredItems
                    .Where(i => printer.ProductIds.Contains(i.ProductId))
                    .ToList();

                if (printer.Categories != null && printer.Categories.Any())
                {
                    filteredItems = filteredItems.Intersect(productIdMatches).ToList();
                }
                else
                {
                    filteredItems = productIdMatches;
                }
            }

            return filteredItems;
        }

        private byte[] GenerateReceiptPdf(Order order, int orderNumber, int invoiceNumber)
        {
            try
            {
                var logoPath = Path.Combine(_env.WebRootPath, "images", "logo.png");
                var yekanRegular = Path.Combine(_env.WebRootPath, "fonts", "YEKANBAKHMEDIUM.TTF");
                var yekanBold = Path.Combine(_env.WebRootPath, "fonts", "YEKANBAKHBOLD.TTF");

                if (!File.Exists(yekanRegular))
                {
                    yekanRegular = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "Tahoma.ttf");
                }
                if (!File.Exists(yekanBold))
                {
                    yekanBold = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "Tahoma.ttf");
                }

                using var renderer = new ReceiptImageRenderer(yekanRegular, yekanBold);

                var img = renderer.Render(
                    order: order,
                    invoiceNumber: invoiceNumber,
                    logoPhysicalPath: logoPath,
                    now: DateTime.Now,
                    receiptWidthPx: 640,
                    title: ResolveOrderTypeTitle(order),
                    headerTitle: "فیش",
                    addressLine: "از خرید شما متشکریم",
                    tableNumber: order.TableNumber ?? 0,
                    discount: 0
                );

                return BuildPdfFromImage(img.ImageBytes, img.WidthPx, img.HeightPx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating receipt PDF");
                return null;
            }
        }

        private byte[] GenerateKitchenReceiptPdf(Order order, int orderNumber, int invoiceNumber,
            PrinterSetting printer, List<OrderItem> filteredItems)
        {
            try
            {
                var yekanRegular = Path.Combine(_env.WebRootPath, "fonts", "YEKANBAKHMEDIUM.TTF");
                var yekanBold = Path.Combine(_env.WebRootPath, "fonts", "YEKANBAKHBOLD.TTF");

                if (!File.Exists(yekanRegular))
                {
                    yekanRegular = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "Tahoma.ttf");
                }
                if (!File.Exists(yekanBold))
                {
                    yekanBold = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "Tahoma.ttf");
                }

                var filteredOrder = new Order
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    OrderType = order.OrderType,
                    OrderItems = filteredItems,
                    TotalAmount = filteredItems.Sum(i => i.Quantity * i.UnitPrice),
                    TableNumber = order.TableNumber,
                    CustomerName = order.CustomerName
                };

                using var renderer = new ReceiptImageRenderer(yekanRegular, yekanBold);

                var printerTitle = $"آشپزخانه - {printer.Name}";
                if (printer.Categories != null && printer.Categories.Any())
                {
                    printerTitle += $" [{string.Join(", ", printer.Categories)}]";
                }

                var img = renderer.Render(
                    order: filteredOrder,
                    invoiceNumber: invoiceNumber,
                    logoPhysicalPath: "",
                    now: DateTime.Now,
                    receiptWidthPx: 640,
                    title: printerTitle,
                    headerTitle: "پیش‌فاکتور",
                    addressLine: null,
                    tableNumber: order.TableNumber ?? 0,
                    discount: 0
                );

                return BuildPdfFromImage(img.ImageBytes, img.WidthPx, img.HeightPx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating kitchen receipt PDF for printer {PrinterName}", printer.Name);
                return null;
            }
        }

        private byte[] GenerateLabelPdf(Order order, int orderNumber, PrinterSetting printer)
        {
            try
            {
                var html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <title>Label - Order {orderNumber}</title>
                    <style>
                        body {{ 
                            font-family: Tahoma, sans-serif; 
                            padding: 10px;
                            margin: 0;
                            direction: rtl;
                        }}
                        .label {{
                            border: 1px solid #ccc;
                            padding: 15px;
                            margin-bottom: 10px;
                            border-radius: 5px;
                        }}
                        .order-no {{ font-size: 16px; font-weight: bold; }}
                        .items {{ margin-top: 10px; }}
                        .item {{ margin: 5px 0; }}
                    </style>
                </head>
                <body>
                    <div class='label'>
                        <div class='order-no'>سفارش شماره: {orderNumber}</div>
                        <div class='order-type'>نوع: {(order.OrderType == "TakeAway" ? "تعویض سری" : "سفارش جدید")}</div>
                        <div class='items'>
                            {string.Join("", order.OrderItems.Select(i => $"<div class='item'>• {i.Quantity}x {i.Product?.Name}</div>"))}
                        </div>
                    </div>
                </body>
                </html>";

                return GeneratePdfFromHtml(html);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating label PDF");
                return null;
            }
        }

        private byte[] GeneratePdfFromHtml(string html)
        {
            try
            {
                double widthInPoints = 80.0 * 72.0 / 25.4;
                double heightInPoints = 300.0 * 72.0 / 25.4;

                var pdfBytes = Pdf
                    .From(html)
                    .WithGlobalSetting("page-size", "Custom")
                    .WithGlobalSetting("page-width", $"{widthInPoints:F2}pt")
                    .WithGlobalSetting("page-height", $"{heightInPoints:F2}pt")
                    .WithGlobalSetting("margin-top", "0")
                    .WithGlobalSetting("margin-bottom", "0")
                    .WithGlobalSetting("margin-left", "0")
                    .WithGlobalSetting("margin-right", "0")
                    .WithGlobalSetting("disable-smart-shrinking", "true")
                    .Content();

                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF from HTML");
                return null;
            }
        }

        private byte[] BuildPdfFromImage(byte[] jpegBytes, int imgWpx, int imgHpx)
        {
            const float paperWidthMm = 75f;
            float pageWpt = paperWidthMm / 25.4f * 72f;
            float scale = pageWpt / imgWpx;
            float pageHpt = imgHpx * scale;

            using var stream = new MemoryStream();
            var offsets = new List<long> { 0 };

            void W(string s) => stream.Write(Encoding.ASCII.GetBytes(s));
            void AddObj(string body) { offsets.Add(stream.Position); W(body); W("\n"); }
            void AddStreamObj(string dict, byte[] bytes)
            {
                offsets.Add(stream.Position);
                W(dict);
                W("\nstream\n");
                stream.Write(bytes);
                W("\nendstream\nendobj\n");
            }

            W("%PDF-1.4\n");
            AddObj("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj");
            AddObj("2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj");
            AddObj($"3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 {pageWpt:F2} {pageHpt:F2}] " +
                   "/Resources << /XObject << /Im1 4 0 R >> >> /Contents 5 0 R >> endobj");
            AddStreamObj($"4 0 obj << /Type /XObject /Subtype /Image /Width {imgWpx} /Height {imgHpx} " +
                         $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {jpegBytes.Length} >>", jpegBytes);

            var content = $"q {pageWpt:F2} 0 0 {pageHpt:F2} 0 0 cm /Im1 Do Q";
            var contentBytes = Encoding.ASCII.GetBytes(content);
            AddStreamObj($"5 0 obj << /Length {contentBytes.Length} >>", contentBytes);

            var xrefStart = stream.Position;
            W("xref\n"); W($"0 {offsets.Count}\n"); W("0000000000 65535 f \n");
            for (int i = 1; i < offsets.Count; i++) W($"{offsets[i]:D10} 00000 n \n");
            W($"trailer << /Size {offsets.Count} /Root 1 0 R >>\n");
            W("startxref\n"); W($"{xrefStart}\n"); W("%%EOF\n");

            return stream.ToArray();
        }

        private byte[] CreateTestPdf()
        {
            var html = @"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <title>Test Print</title>
                <style>
                    body { font-family: 'Tahoma', sans-serif; padding: 20px; }
                    h2 { color: #333; }
                    p { color: #666; }
                </style>
            </head>
            <body>
                <h2>🧪 Test Print - Aghadon POS</h2>
                <p>This is a test print from your POS system.</p>
                <p>Date: " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + @"</p>
                <hr/>
                <p style='color: green;'>✅ If you see this, your printer is working correctly!</p>
            </body>
            </html>";

            return GeneratePdfFromHtml(html);
        }

        private static string ResolveOrderTypeTitle(Order order)
        {
            return order.OrderType switch
            {
                "TakeAway" => "تعویض سری",
                "EatIn" => "سفارش جدید",
                _ => order.OrderType ?? "سفارش"
            };
        }
    }
}