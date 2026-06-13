using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using KioskCenter.Models;

namespace KioskCenter.Helper
{

    public sealed class ReceiptImageRenderer : IDisposable
    {
        private readonly PrivateFontCollection _pfc = new();
        private readonly FontFamily _fontFamily;

        public ReceiptImageRenderer(string yekanRegularTtfPath, string yekanBoldTtfPath)
        {
            if (File.Exists(yekanRegularTtfPath)) _pfc.AddFontFile(yekanRegularTtfPath);
            if (File.Exists(yekanBoldTtfPath)) _pfc.AddFontFile(yekanBoldTtfPath);

            _fontFamily = _pfc.Families.FirstOrDefault() ?? FontFamily.GenericSansSerif;
        }

        public (byte[] ImageBytes, int WidthPx, int HeightPx) Render(
            Order order,
            int invoiceNumber,
            string logoPhysicalPath,
            DateTime now,
            int receiptWidthPx = 640,
            string title = "بیرون بر",
            string headerTitle = "فیش",
            string? addressLine = null,
            int? tableNumber = 0,
            long discount = 0)
        {
            int width = receiptWidthPx;
            int padding = (int)Math.Round(width * 0.04);
            int radius = (int)Math.Round(width * 0.03);
            int topMargin = (int)Math.Round(width * 0.035);
            int gap = (int)Math.Round(width * 0.035);

            // فونت‌ها (مثل رسید: تیتر بزرگ، تیتر متوسط، متن)
            using var fTitle = new Font(_fontFamily, width * 0.10f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var fH1 = new Font(_fontFamily, width * 0.060f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var fH2 = new Font(_fontFamily, width * 0.045f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var fText = new Font(_fontFamily, width * 0.038f, FontStyle.Regular, GraphicsUnit.Pixel);
            using var fBold = new Font(_fontFamily, width * 0.038f, FontStyle.Bold, GraphicsUnit.Pixel);

            int itemsCount = order.OrderItems.Count;

            // اندازه‌های بلوک‌ها
            int logoSize = (int)Math.Round(width * 0.40);
            int orderBoxW = (int)Math.Round(width * 0.23);
            int orderBoxH = (int)Math.Round(width * 0.15);

            int headerTopH = Math.Max(logoSize, orderBoxH); // هم‌تراز کردن لوگو و شماره سفارش

            int titleBoxH = (int)Math.Round(width * 0.20); // کادر عنوان (بالا متا + وسط تیتر)
            int headerH = (int)Math.Round(width * 0.09);
            int rowH = (int)Math.Round(width * 0.095);

            int discountBoxH = (int)Math.Round(width * 0.12);
            int payBoxH = (int)Math.Round(width * 0.14);
            int addressBoxH = (int)Math.Round(width * 0.15);

            // محاسبه ارتفاع کل (بدون دوباره‌کاری y)
            int height =
                topMargin +
                headerTopH + gap +
                titleBoxH + gap +
                headerH + itemsCount * rowH + gap +
                discountBoxH + gap +
                (int)Math.Round(width * 0.07) + // فاصله و تیتر "قابل پرداخت"
                payBoxH + gap +
                addressBoxH +
                topMargin;

            using var bmp = new Bitmap(width, height);
            bmp.SetResolution(1200, 1200);
            using var g = Graphics.FromImage(bmp);

            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.HighQuality; // جایگزین AntiAlias
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit; // برای متن‌های فارسی در ویندوز عالی است
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;


      

            using var pen = new Pen(Color.Black, Math.Max(2, width * 0.006f));
            using var thinPen = new Pen(Color.Black, Math.Max(1, width * 0.003f));
            using var black = new SolidBrush(Color.Black);
            using var white = new SolidBrush(Color.White);

            // RTL
            using var sfRtl = new StringFormat(StringFormat.GenericTypographic)
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoClip
            };

            using var sfCenterRtl = new StringFormat(StringFormat.GenericTypographic)
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoClip
            };

            using var sfNearRtl = new StringFormat(StringFormat.GenericTypographic)
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoClip
            };

            // LTR برای "تومان" داخل باکس مشکی
            using var sfLtr = new StringFormat(StringFormat.GenericTypographic)
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoClip
            };

            int y = topMargin;

            // --- هدر: لوگو راست + شماره سفارش چپ (هم‌تراز) ---
            if (File.Exists(logoPhysicalPath))
            {
                using var logo = Image.FromFile(logoPhysicalPath);
                var logoRect = new Rectangle(width - padding - logoSize, y, logoSize, logoSize);
                g.DrawImage(logo, logoRect);
            }

            var orderBox = new Rectangle(
                padding,
                y + (headerTopH - orderBoxH) / 2,
                orderBoxW,
                orderBoxH
            );
            DrawRoundRect(g, thinPen, orderBox, (int)(radius * 0.8));
            g.DrawString(ToFaDigits(invoiceNumber.ToString()), fH1, black, orderBox, sfCenterRtl);

            y += headerTopH + gap;

            // --- کادر عنوان: بالا متا (تاریخ/ساعت/میز) + پایین تیتر بزرگ ---
            var titleBox = new Rectangle(padding, y, width - padding * 2, titleBoxH);
            DrawRoundRect(g, pen, titleBox, radius);

            int metaH = (int)(titleBoxH * 0.40f);
            var metaRect = new Rectangle(titleBox.X + 16, titleBox.Y + 6, titleBox.Width - 32, metaH);

            // سه ستون متا
            float rightW = metaRect.Width * 0.38f;  // تاریخ
            float midW = metaRect.Width * 0.24f;  // ساعت
            float leftW = metaRect.Width - (rightW + midW); // میز

            var rectRight = new RectangleF(metaRect.Right - rightW, metaRect.Y, rightW, metaRect.Height);
            var rectMid = new RectangleF(metaRect.Right - (rightW + midW), metaRect.Y, midW, metaRect.Height);
            var rectLeft = new RectangleF(metaRect.X, metaRect.Y, leftW, metaRect.Height);

            // تاریخ و ساعت
            string dateFa = ToPersianDate(now); // فقط تاریخ
            string timeFa = ToFaDigits(now.ToString("HH:mm", CultureInfo.InvariantCulture));
            g.DrawString(dateFa, fBold, black, rectRight, sfRtl);
            g.DrawString(timeFa, fBold, black, rectMid, sfCenterRtl);

            // میز (اگر داشت)
            string miz = tableNumber.HasValue
                ? $"شماره میز: {ToFaDigits(tableNumber.Value.ToString())}"
                : "";
            g.DrawString(miz, fBold, black, rectLeft, sfNearRtl);

            // تیتر بزرگ وسط
            var bigTitleRect = new Rectangle(titleBox.X, titleBox.Y + metaH, titleBox.Width, titleBoxH - metaH);
            g.DrawString(title, fTitle, black, bigTitleRect, sfCenterRtl);

            y += titleBoxH + gap;

            // --- جدول: راست به چپ (نام غذا | تعداد | قیمت | مبلغ) ---
            int tableX = padding;
            int tableW = width - padding * 2;

            int colName = (int)(tableW * 0.44);
            int colQty = (int)(tableW * 0.16);
            int colUnit = (int)(tableW * 0.20);
            int colSum = tableW - (colName + colQty + colUnit);

            // چیدمان ستون‌ها (از چپ به راست) ولی متن RTL
            int xSum = tableX;
            int xUnit = xSum + colSum;
            int xQty = xUnit + colUnit;
            int xName = xQty + colQty;

            // هدر جدول (مثل رسید: خطوط ظریف)
            var hdrRect = new Rectangle(tableX, y, tableW, headerH);
            g.DrawRectangle(thinPen, hdrRect);

            // خطوط عمودی هدر (اگر خیلی شلوغ شد، کامنت کنید)
            g.DrawLine(thinPen, xUnit, y, xUnit, y + headerH);
            g.DrawLine(thinPen, xQty, y, xQty, y + headerH);
            g.DrawLine(thinPen, xName, y, xName, y + headerH);

            g.DrawString("مبلغ", fBold, black, new RectangleF(xSum, y, colSum, headerH), sfCenterRtl);
            g.DrawString("قیمت", fBold, black, new RectangleF(xUnit, y, colUnit, headerH), sfCenterRtl);
            g.DrawString("تعداد", fBold, black, new RectangleF(xQty, y, colQty, headerH), sfCenterRtl);
            g.DrawString("نام", fBold, black, new RectangleF(xName, y, colName, headerH), sfCenterRtl);

            y += headerH;

            // برای نام غذا بهتره NoWrap باشه تا ردیف به‌هم نریزه
            using var sfRtlNoWrap = (StringFormat)sfRtl.Clone();
            sfRtlNoWrap.FormatFlags |= StringFormatFlags.NoWrap;

            foreach (var item in order.OrderItems)
            {
                long unit = (long)item.UnitPrice;
                long qty = item.Quantity;
                long sum = unit * qty;

                var rowRect = new Rectangle(tableX, y, tableW, rowH);
                g.DrawRectangle(thinPen, rowRect);

                // خطوط عمودی ردیف (اگر شلوغ شد، کامنت کنید)
                g.DrawLine(thinPen, xUnit, y, xUnit, y + rowH);
                g.DrawLine(thinPen, xQty, y, xQty, y + rowH);
                g.DrawLine(thinPen, xName, y, xName, y + rowH);

                g.DrawString(ToFaMoney(sum), fBold, black, new RectangleF(xSum, y, colSum, rowH), sfCenterRtl);
                g.DrawString(ToFaMoney(unit), fBold, black, new RectangleF(xUnit, y, colUnit, rowH), sfCenterRtl);
                g.DrawString(ToFaDigits(qty.ToString()), fBold, black, new RectangleF(xQty, y, colQty, rowH), sfCenterRtl);

                g.DrawString(item.Product.Name ?? "", fBold, black,
                    new RectangleF(xName + 10, y, colName - 20, rowH), sfRtlNoWrap);

                y += rowH;
            }

            y += gap;

            // --- تخفیف ---
            var discountBox = new Rectangle(padding, y, width - padding * 2, discountBoxH);
            DrawRoundRect(g, pen, discountBox, radius);

            // راست: "تخفیف :" / چپ: مبلغ
            g.DrawString(ToFaMoney(discount), fBold, black,
                new RectangleF(discountBox.X + 18, discountBox.Y, discountBox.Width * 0.35f, discountBoxH), sfRtl);

            g.DrawString("تخفیف :", fBold, black,
                new RectangleF(discountBox.X + discountBox.Width * 0.40f, discountBox.Y, discountBox.Width * 0.58f, discountBoxH), sfRtl);

            y += discountBoxH + gap;

            // --- قابل پرداخت ---
            g.DrawString("قابل پرداخت", fH2, black,
                new RectangleF(padding, y, width - padding * 2, width * 0.07f), sfRtl);

            y += (int)Math.Round(width * 0.07);

            long payable = (long)order.TotalAmount - discount;
            if (payable < 0) payable = 0;

            int payBoxW = (int)Math.Round((width - padding * 2) * 0.60);
            var payBox = new Rectangle(padding, y, payBoxW, payBoxH);

            using (var blackFill = new SolidBrush(Color.Black))
                g.FillRectangle(blackFill, payBox);

            // "تومان" سمت چپ باکس مشکی
            g.DrawString("تومان", fBold, white,
                new RectangleF(payBox.X + 14, payBox.Y, payBoxW * 0.24f, payBoxH), sfLtr);

            // مبلغ سمت راست (RTL)
            g.DrawString(ToFaMoney(payable), fH1, white,
                new RectangleF(payBox.X + payBoxW * 0.26f, payBox.Y, payBoxW * 0.72f, payBoxH), sfRtl);

            y += payBoxH + gap;

            // --- آدرس ---
            var addr = addressLine ?? "بلوار ابوذر بین پل چهارم و پنجم";
            var addrBox = new Rectangle(padding, y, width - padding * 2, addressBoxH);
            DrawRoundRect(g, pen, addrBox, radius);
            g.DrawString(addr, fH2, black, addrBox, sfCenterRtl);

            // --- خروجی JPEG ---
            using var ms = new MemoryStream();
            var enc = GetJpegEncoder();
            if (enc is null)
            {
                bmp.Save(ms, ImageFormat.Jpeg);
            }
            else
            {
                using var ep = new EncoderParameters(1);
                ep.Param[0] = new EncoderParameter(Encoder.Quality, 92L);
                bmp.Save(ms, enc, ep);
            }

            return (ms.ToArray(), width, height);
        }

        private static void DrawRoundRect(Graphics g, Pen p, Rectangle r, int radius)
        {
            int d = radius * 2;
            using var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            g.DrawPath(p, path);
        }

        private static ImageCodecInfo? GetJpegEncoder()
            => ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);

        private static string ToFaDigits(string input)
        {
            char[] fa = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'];
            var sb = new System.Text.StringBuilder(input.Length);
            foreach (var ch in input)
            {
                if (ch >= '0' && ch <= '9') sb.Append(fa[ch - '0']);
                else sb.Append(ch);
            }
            return sb.ToString();
        }

        private static string ToPersianDate(DateTime dateTime)
        {
            var p = new PersianCalendar();
            int y = p.GetYear(dateTime);
            int m = p.GetMonth(dateTime);
            int d = p.GetDayOfMonth(dateTime);
            return ToFaDigits($"{y:0000}/{m:00}/{d:00}");
        }

        private static string ToFaMoney(long value)
        {
            var formatted = value.ToString("#,0", CultureInfo.InvariantCulture);
            return ToFaDigits(formatted);
        }
        // اضافه کنید به ReceiptImageRenderer.cs - قبل از آخرین } 

        public (byte[] ImageBytes, int WidthPx, int HeightPx) RenderReport(
      List<OrderReportDto> orders,
      string title,
      string period,
      decimal totalSales,
      decimal totalTax,
      int totalOrders,
      decimal averageOrderValue,
      DateTime now,
      int reportWidthPx = 800)
        {
            int width = reportWidthPx;
            int padding = (int)Math.Round(width * 0.04);
            int gap = (int)Math.Round(width * 0.025);
            int topMargin = (int)Math.Round(width * 0.03);

            // افزایش سایز فونت‌ها
            using var fTitle = new Font(_fontFamily, width * 0.1f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var fH1 = new Font(_fontFamily, width * 0.07f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var fH2 = new Font(_fontFamily, width * 0.055f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var fText = new Font(_fontFamily, width * 0.048f, FontStyle.Regular, GraphicsUnit.Pixel);
            using var fBold = new Font(_fontFamily, width * 0.048f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var fBig = new Font(_fontFamily, width * 0.08f, FontStyle.Bold, GraphicsUnit.Pixel);

            int headerH = (int)Math.Round(width * 0.12);
            int rowH = (int)Math.Round(width * 0.11);
            int statsBoxH = (int)Math.Round(width * 0.16);

            int height = topMargin + headerH + gap + statsBoxH + gap +
                         orders.Count * rowH + gap + (int)Math.Round(width * 0.15) + topMargin;

            using var bmp = new Bitmap(width, height);
            bmp.SetResolution(1200, 1200);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            using var pen = new Pen(Color.Black, Math.Max(2, width * 0.003f));
            using var thinPen = new Pen(Color.Black, Math.Max(1, width * 0.0015f));
            using var black = new SolidBrush(Color.Black);
            using var grayBg = new SolidBrush(Color.FromArgb(245, 245, 245));

            var sfCenter = new StringFormat(StringFormat.GenericTypographic)
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            var sfRtl = new StringFormat(StringFormat.GenericTypographic)
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.DirectionRightToLeft
            };

            int y = topMargin;

            // هدر گزارش
            g.DrawString(title, fTitle, black, new RectangleF(0, y, width, headerH), sfCenter);
            y += headerH;

            g.DrawString(period, fH2, black, new RectangleF(0, y, width, (int)(headerH * 0.7f)), sfCenter);
            y += (int)(headerH * 0.7f) + gap;

            // آمار کلی - با رنگ‌بندی بهتر و اندازه بزرگتر
            int statsW = (width - padding * 5) / 4;
            string[] statsLabels = { "تعداد سفارش", "مجموع فروش", "مالیات", "میانگین سفارش" };
            string[] statsValues = {
        totalOrders.ToString(),
        $"{ToFaMoney((long)totalSales)}",
        $"{ToFaMoney((long)totalTax)}",
        $"{ToFaMoney((long)averageOrderValue)}"
    };

            Color[] boxColors = {
        Color.FromArgb(52, 152, 219),  // آبی
        Color.FromArgb(46, 204, 113),  // سبز
        Color.FromArgb(241, 196, 15),  // زرد
        Color.FromArgb(155, 89, 182)   // بنفش
    };

            for (int i = 0; i < 4; i++)
            {
                var box = new Rectangle(padding + i * (statsW + padding), y, statsW, statsBoxH);

                // باکس با رنگ زمینه
                using var bgBrush = new SolidBrush(Color.FromArgb(20, boxColors[i]));
                g.FillRectangle(bgBrush, box);
                DrawRoundRect(g, new Pen(boxColors[i], 2), box, (int)(width * 0.02));

                // عنوان
                using var fLabel = new Font(_fontFamily, width * 0.04f, FontStyle.Regular, GraphicsUnit.Pixel);
                g.DrawString(statsLabels[i], fLabel, new SolidBrush(boxColors[i]),
                    new RectangleF(box.X, box.Y + 8, statsW, statsBoxH * 0.3f), sfCenter);

                // مقدار
                using var fValue = new Font(_fontFamily, width * 0.055f, FontStyle.Bold, GraphicsUnit.Pixel);
                g.DrawString(statsValues[i], fValue, black,
                    new RectangleF(box.X, box.Y + statsBoxH * 0.35f, statsW, statsBoxH * 0.55f), sfCenter);
            }
            y += statsBoxH + gap;

            // جدول سفارشات - بدون ستون ردیف
            int tableX = padding;
            int tableW = width - padding * 2;

            // تنظیم عرض ستون‌ها (بدون ستون ردیف)
            int[] cols = {
        (int)(tableW * 0.22),   // شماره فاکتور
        (int)(tableW * 0.38),   // تاریخ (بزرگتر)
        (int)(tableW * 0.16),   // نوع
        (int)(tableW * 0.24)    // مبلغ
    };

            int[] xPos = new int[4];
            xPos[3] = tableX;
            for (int i = 2; i >= 0; i--) xPos[i] = xPos[i + 1] + cols[i + 1];

            // Header
            var headerRect = new Rectangle(tableX, y, tableW, rowH);
            g.FillRectangle(grayBg, headerRect);
            g.DrawRectangle(pen, headerRect);

            string[] headers = { "شماره فاکتور", "تاریخ", "نوع", "مبلغ (تومان)" };
            for (int i = 0; i < headers.Length; i++)
            {
                var rect = new RectangleF(xPos[i], y, cols[i], rowH);
                g.DrawString(headers[i], fBold, black, rect, sfCenter);
                if (i < headers.Length - 1)
                    g.DrawLine(thinPen, xPos[i + 1], y, xPos[i + 1], y + rowH);
            }
            y += rowH;

            // داده‌های جدول - با تاریخ بزرگ
            foreach (var order in orders)
            {
                var rowRect = new Rectangle(tableX, y, tableW, rowH);
                g.DrawRectangle(thinPen, rowRect);

                // تاریخ کامل شمسی با فرمت خوانا
                string persianDate = ToPersianDateFull(order.OrderDate);
                string orderType = order.OrderType == "EatIn" ? "حضوری" : "بیرون بر";
                string amount = ToFaMoney((long)order.TotalAmount);

                string[] rowData = {
            ToFaDigits(order.OrderNumber.ToString()),
            persianDate,
            orderType,
            amount
        };

                for (int i = 0; i < rowData.Length; i++)
                {
                    var rect = new RectangleF(xPos[i], y, cols[i], rowH);
                    if (i == 1) // ستون تاریخ با فونت بزرگتر
                    {
                        using var fDate = new Font(_fontFamily, width * 0.042f, FontStyle.Regular, GraphicsUnit.Pixel);
                        g.DrawString(rowData[i], fDate, black, rect, sfCenter);
                    }
                    else
                    {
                        g.DrawString(rowData[i], fText, black, rect, sfCenter);
                    }
                    if (i < rowData.Length - 1)
                        g.DrawLine(thinPen, xPos[i + 1], y, xPos[i + 1], y + rowH);
                }
                y += rowH;
            }

            y += gap;

            // جمع کل (مجموع) - در پایین جدول
            int totalBoxW = (int)(width * 0.5);
            int totalBoxX = width - padding - totalBoxW;
            var totalBox = new Rectangle(totalBoxX, y, totalBoxW, (int)(rowH * 1.2));

            using var totalBg = new SolidBrush(Color.FromArgb(240, 248, 255));
            g.FillRectangle(totalBg, totalBox);
            DrawRoundRect(g, new Pen(Color.FromArgb(52, 152, 219), 2), totalBox, (int)(width * 0.015));

            // عنوان جمع کل
            using var fTotalLabel = new Font(_fontFamily, width * 0.048f, FontStyle.Bold, GraphicsUnit.Pixel);
            g.DrawString("جمع کل فروش:", fTotalLabel, new SolidBrush(Color.FromArgb(52, 152, 219)),
                new RectangleF(totalBox.X + 10, totalBox.Y, totalBoxW * 0.45f, totalBox.Height), sfRtl);

            // مبلغ جمع کل
            using var fTotalValue = new Font(_fontFamily, width * 0.055f, FontStyle.Bold, GraphicsUnit.Pixel);
            g.DrawString(ToFaMoney((long)totalSales), fTotalValue, new SolidBrush(Color.FromArgb(46, 204, 113)),
                new RectangleF(totalBox.X + totalBoxW * 0.45f, totalBox.Y, totalBoxW * 0.5f, totalBox.Height), sfRtl);

            y += totalBox.Height + gap;

            // فوتر
            var footerY = height - topMargin - (int)Math.Round(width * 0.07f);
            string footerText = $"تاریخ چاپ: {ToPersianDateFull(now)} - ساعت: {ToFaDigits(now.ToString("HH:mm"))}";
            using var fFooter = new Font(_fontFamily, width * 0.04f, FontStyle.Regular, GraphicsUnit.Pixel);
            g.DrawString(footerText, fFooter, black,
                new RectangleF(padding, footerY, width - padding * 2, topMargin), sfCenter);

            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Jpeg);
            return (ms.ToArray(), width, height);
        }

        // متد تاریخ کامل
        private string ToPersianDateFull(DateTime dateTime)
        {
            var p = new PersianCalendar();
            int y = p.GetYear(dateTime);
            int m = p.GetMonth(dateTime);
            int d = p.GetDayOfMonth(dateTime);

            string monthName = GetPersianMonthName(m);
            string dayOfWeek = GetPersianDayOfWeek(p.GetDayOfWeek(dateTime));

            // فرمت: سه‌شنبه 22 خرداد 1404
            return $"{dayOfWeek} {ToFaDigits(d.ToString())} {monthName} {ToFaDigits(y.ToString())}";
        }

        // متد کمکی برای نام ماه فارسی
        private string GetPersianMonthName(int month)
        {
            string[] monthNames = {
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    };
            return monthNames[month - 1];
        }

        // متد کمکی برای نام روز فارسی
        private string GetPersianDayOfWeek(DayOfWeek dayOfWeek)
        {
            string[] dayNames = {
        "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه"
    };
            return dayNames[(int)dayOfWeek];
        }
        public void Dispose() => _pfc.Dispose();
    }

}
