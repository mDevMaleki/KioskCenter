using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Globalization;
using KioskCenter.Helper;
using KioskCenter.Data;
using KioskCenter.Interfaces;
using KioskCenter.Models;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReceiptController : ControllerBase
    {
        private readonly CoffeeShopContext _context;
        private readonly IReceiptPrinter _receiptPrinter;
        private readonly ILogger<ReceiptController> _logger;

        public ReceiptController(
            CoffeeShopContext context,
            IReceiptPrinter receiptPrinter,
            ILogger<ReceiptController> logger)
        {
            _context = context;
            _receiptPrinter = receiptPrinter;
            _logger = logger;
        }


        [HttpGet("print/{orderId}")]
        public async Task<IActionResult> PrintAsync(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(p => p.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order is null)
                {
                    _logger.LogWarning("Order {OrderId} not found", orderId);
                    return NotFound(new { success = false, message = "سفارش پیدا نشد." });
                }

                // اضافه کنید: مسیرهای فونت را بررسی کنید
                var fontsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts");
                if (!Directory.Exists(fontsDir))
                {
                    _logger.LogWarning("Fonts directory not found at {FontsDir}", fontsDir);
                    return StatusCode(500, new { success = false, message = "پوشه فونت ها یافت نشد." });
                }

                var printed = _receiptPrinter.TryPrint(order, order.Id, order.OrderNumber);

                if (!printed)
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "چاپ انجام نشد. لطفا وضعیت چاپگر را بررسی کنید."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "فیش با موفقیت چاپ شد."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در چاپ فیش سفارش {OrderId}", orderId);
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }


        [HttpPost("print-report")]
        public async Task<IActionResult> PrintReportAsync([FromBody] PrintReportRequest request)
        {
            try
            {
                if (request.Orders == null || !request.Orders.Any())
                    return BadRequest(new { success = false, message = "هیچ سفارشی برای چاپ وجود ندارد." });

                var pdfBytes = await GenerateReportPdf(request);

                if (pdfBytes == null || pdfBytes.Length == 0)
                    return StatusCode(500, new { success = false, message = "خطا در ایجاد فایل گزارش." });

                if (request.PrintDirectly)
                {
                    var printed = _receiptPrinter.PrintReport(pdfBytes);
                    if (!printed)
                        return StatusCode(500, new { success = false, message = "چاپ گزارش انجام نشد." });

                    return Ok(new { success = true, message = "گزارش با موفقیت چاپ شد." });
                }

                return File(pdfBytes, "application/pdf", $"report-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در چاپ گزارش");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // کلاس Request - بدون کلاس‌های DTO داخلی
        public class PrintReportRequest
        {
            public List<OrderReportDto> Orders { get; set; } = new();
            public string ReportTitle { get; set; } = "گزارش فروش";
            public string ReportPeriod { get; set; } = "";
            public decimal TotalSales { get; set; }
            public decimal TotalTax { get; set; }
            public int TotalOrders { get; set; }
            public decimal AverageOrderValue { get; set; }
            public bool PrintDirectly { get; set; } = false;
        }

        private async Task<byte[]?> GenerateReportPdf(PrintReportRequest request)
        {
            try
            {
                var yekanRegular = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "YEKANBAKHMEDIUM.TTF");
                var yekanBold = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "YEKANBAKHBOLD.TTF");

                using var renderer = new ReceiptImageRenderer(yekanRegular, yekanBold);

                var img = renderer.RenderReport(
                    orders: request.Orders,
                    title: request.ReportTitle,
                    period: request.ReportPeriod,
                    totalSales: request.TotalSales,
                    totalTax: request.TotalTax,
                    totalOrders: request.TotalOrders,
                    averageOrderValue: request.AverageOrderValue,
                    now: DateTime.Now
                );

                var pdfBytes = BuildPdfFromImage(img.ImageBytes, img.WidthPx, img.HeightPx);
                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در تولید PDF گزارش");
                return null;
            }
        }

        private static byte[] BuildPdfFromImage(byte[] jpegBytes, int imgWpx, int imgHpx)
        {
            const float paperWidthMm = 210f;
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
                W(dict); W("\nstream\n");
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
    }
}