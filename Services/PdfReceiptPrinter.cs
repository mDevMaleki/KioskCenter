using System.Text;
using KioskCenter.Helper;
using KioskCenter.Interfaces;
using KioskCenter.Models;
using Microsoft.Extensions.Options;

namespace KioskCenter.Services
{
    public sealed class PdfReceiptPrinter : IReceiptPrinter
    {
        private readonly IWebHostEnvironment _env;
        private readonly ReceiptPrintingOptions _options;

        public PdfReceiptPrinter(IWebHostEnvironment env, ReceiptPrintingOptions options)
        {
            _env = env;
            _options = options;
        }

        public bool TryPrint(Order order, int orderNumber, int invoiceNumber)
        {
            _ = CreatePdf(order, orderNumber, invoiceNumber);
            return true;
        }

        public string CreatePdf(Order order, int orderNumber, int invoiceNumber)
        {
            // ✅ /images/logo.png یعنی: wwwroot/images/logo.png
            var logoPath = Path.Combine(_env.WebRootPath, "images", "logo.png");

            // ✅ فونت‌ها را زیر wwwroot/fonts قرار دهید
            var yekanRegular = Path.Combine(_env.WebRootPath, "fonts", "YEKANBAKHMEDIUM.TTF");
            var yekanBold = Path.Combine(_env.WebRootPath, "fonts", "YEKANBAKHBOLD.TTF"); // اسم فایل خودتان را دقیق بگذارید

            // عرض مناسب برای 80mm در 203dpi
            const int receiptWidthPx = 640;

            using var renderer = new ReceiptImageRenderer(yekanRegular, yekanBold);

            var img = renderer.Render(
                order: order,
                invoiceNumber: invoiceNumber,
                logoPhysicalPath: logoPath,
                now: DateTime.Now,
                receiptWidthPx: receiptWidthPx,
                title: ResolveOrderTypeTitle(order),
                headerTitle: "فیش",
                addressLine: "بلوار ابوذر بین پل چهارم و پنجم",
                tableNumber: 0,
                discount: 0
            );

            var pdfBytes = BuildPdfFromImage_80mm(img.ImageBytes, img.WidthPx, img.HeightPx);

            var fileName = $"receipt-{orderNumber}-{DateTime.Now:yyyyMMddHHmmss}.pdf";
            var outputDirectory = ResolveOutputDirectory();
            Directory.CreateDirectory(outputDirectory);
            var path = Path.Combine(outputDirectory, fileName);
            File.WriteAllBytes(path, pdfBytes);
            return path;
        }

        private string ResolveOutputDirectory()
        {
            if (!string.IsNullOrWhiteSpace(_options.OutputDirectory))
            {
                return _options.OutputDirectory;
            }
            Console.WriteLine(Path.Combine(_env.ContentRootPath, "App_Data", "receipts"));
            return Path.Combine(_env.ContentRootPath, "App_Data", "receipts");
        }

        private static string ResolveOrderTypeTitle(Order order)
        {
            return order.OrderType switch
            {
                "TakeAway" => "بیرون‌بر",
                "EatIn" => "داخل سالن",
                _ => order.OrderType
            };
        }

        private static byte[] BuildPdfFromImage_80mm(byte[] jpegBytes, int imgWpx, int imgHpx)
        {
            const float paperWidthMm = 75f;
            float pageWpt = paperWidthMm / 25.4f * 72f; // 80mm -> 226.77pt
            float scale = pageWpt / imgWpx;
            float pageHpt = imgHpx * scale;

            using var stream = new MemoryStream();
            var offsets = new List<long> { 0 };

            void W(string s)
            {
                var buf = Encoding.ASCII.GetBytes(s);
                stream.Write(buf, 0, buf.Length);
            }

            void AddObj(string body)
            {
                offsets.Add(stream.Position);
                W(body);
                W("\n");
            }

            void AddStreamObj(string dict, byte[] bytes)
            {
                offsets.Add(stream.Position);
                W(dict);
                W("\nstream\n");
                stream.Write(bytes, 0, bytes.Length);
                W("\nendstream\nendobj\n");
            }

            W("%PDF-1.4\n");
            AddObj("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj");
            AddObj("2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj");

            AddObj($"3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 {pageWpt:F2} {pageHpt:F2}] " +
                   "/Resources << /XObject << /Im1 4 0 R >> >> /Contents 5 0 R >> endobj");

            AddStreamObj($"4 0 obj << /Type /XObject /Subtype /Image /Width {imgWpx} /Height {imgHpx} " +
                         $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {jpegBytes.Length} >>",
                         jpegBytes);

            var content = $"q {pageWpt:F2} 0 0 {pageHpt:F2} 0 0 cm /Im1 Do Q";
            var contentBytes = Encoding.ASCII.GetBytes(content);
            AddStreamObj($"5 0 obj << /Length {contentBytes.Length} >>", contentBytes);

            var xrefStart = stream.Position;
            W("xref\n");
            W($"0 {offsets.Count}\n");
            W("0000000000 65535 f \n");
            for (int i = 1; i < offsets.Count; i++)
                W($"{offsets[i]:D10} 00000 n \n");

            W($"trailer << /Size {offsets.Count} /Root 1 0 R >>\n");
            W("startxref\n");
            W($"{xrefStart}\n");
            W("%%EOF\n");

            return stream.ToArray();
        }

        public bool PrintReport(byte[] pdfBytes)
        {
            throw new NotImplementedException();
        }

        public bool TestPrint(string printerName)
        {
            throw new NotImplementedException();
        }

        public bool PrintToSpecificPrinter(byte[] pdfBytes, string printerName)
        {
            throw new NotImplementedException();
        }
    }

}
