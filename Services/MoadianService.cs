using KioskCenter.Data;
using KioskCenter.Models;
using Microsoft.EntityFrameworkCore;
using Moadian.Dto;

namespace KioskCenter.Services
{
    // سرویس اتصال به سامانه مودیان (سازمان امور مالیاتی کشور)
    // این سرویس فقط در صورتی فعال می‌شود که کاربر در تنظیمات صریحاً IsEnabled را تیک بزند
    public class MoadianService
    {
        private readonly CoffeeShopContext _context;

        public MoadianService(CoffeeShopContext context)
        {
            _context = context;
        }

        public async Task<MoadianSetting> GetSettingsAsync()
        {
            var setting = await _context.MoadianSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new MoadianSetting { IsEnabled = false };
                _context.MoadianSettings.Add(setting);
                await _context.SaveChangesAsync();
            }
            return setting;
        }

        // ارسال فاکتور فروش به سامانه مودیان - فقط در صورت فعال بودن تنظیمات
        // این متد عمداً خطاها را می‌بلعد تا فرآیند فروش هیچ‌وقت به‌خاطر این سرویس خارجی متوقف نشود
        public async Task<bool> TrySendSaleInvoiceAsync(int saleInvoiceId)
        {
            var setting = await GetSettingsAsync();
            if (!setting.IsEnabled)
                return false;

            var invoice = await _context.SaleInvoices
                .Include(i => i.Party)
                .Include(i => i.Items).ThenInclude(it => it.Product)
                .FirstOrDefaultAsync(i => i.Id == saleInvoiceId);

            if (invoice == null)
                return false;

            try
            {
                if (string.IsNullOrWhiteSpace(setting.PublicKeyPem) || string.IsNullOrWhiteSpace(setting.PrivateKeyPem)
                    || string.IsNullOrWhiteSpace(setting.OrgKeyId) || string.IsNullOrWhiteSpace(setting.Username))
                {
                    invoice.MoadianError = "تنظیمات اتصال (کلیدها/شناسه کاربری) کامل نیست";
                    await _context.SaveChangesAsync();
                    return false;
                }

                var moadian = new global::Moadian.Moadian(
                    setting.PublicKeyPem,
                    setting.PrivateKeyPem,
                    setting.OrgKeyId,
                    setting.Username,
                    setting.BaseUrl);

                var token = await moadian.GetToken();
                if (token == null)
                {
                    invoice.MoadianError = "اخذ توکن از سامانه مودیان ناموفق بود";
                    await _context.SaveChangesAsync();
                    return false;
                }

                moadian.SetToken(token);

                var taxId = moadian.GenerateTaxId(invoice.CreatedAt, invoice.Id);
                var invoiceDto = BuildInvoiceDto(invoice, taxId, setting);

                var packet = new Packet(global::Moadian.Constants.PacketType.INVOICE_V01, invoiceDto);
                var response = await moadian.SendInvoice(packet);

                invoice.MoadianTaxId = taxId;
                invoice.MoadianSent = true;
                invoice.MoadianSentAt = DateTime.Now;
                invoice.MoadianReferenceNumber = response?.ToString();
                invoice.MoadianError = null;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                invoice.MoadianError = ex.Message;
                invoice.MoadianSent = false;
                await _context.SaveChangesAsync();
                return false;
            }
        }

        private InvoiceDto BuildInvoiceDto(SaleInvoice invoice, string taxId, MoadianSetting setting)
        {
            var createdAtMs = new DateTimeOffset(invoice.CreatedAt).ToUnixTimeMilliseconds();

            var header = new InvoiceHeaderDto
            {
                taxid = taxId,
                indatim = createdAtMs,
                indati2m = createdAtMs,
                inty = 1,  // نوع فاکتور: فروش کالا/خدمات (طبق دستورالعمل سازمان امور مالیاتی تنظیم شود)
                inno = invoice.Id.ToString(),
                inp = 1,   // الگوی فاکتور: عادی
                ins = 1,   // موضوع فاکتور: فروش کالا
                tins = setting.Username ?? "",
                tob = 1,   // نوع خریدار: حقیقی (در صورت نیاز اصلاح شود)
                bid = invoice.PartyId.ToString(),
                tinb = invoice.Party?.EconomicCode ?? "",
                tprdis = 0,
                tdis = 0,
                tadis = (int)invoice.TotalAmount,
                tvam = (int)invoice.VatAmount,
                todam = 0,
                tbill = (int)invoice.GrandTotal,
                tvop = 0,
                tax17 = 0
            };

            var body = invoice.Items!.Select(item => new InvoiceBodyDto
            {
                sstt = item.Product?.Name ?? "",
                am = (int)item.Quantity,
                mu = "عدد",
                fee = (int)item.UnitPrice,
                prdis = 0,
                dis = 0,
                adis = (int)item.TotalPrice,
                vra = (int)invoice.VatRate,
                vam = (int)Math.Round(item.TotalPrice * invoice.VatRate / 100),
                tsstam = (int)(item.TotalPrice + Math.Round(item.TotalPrice * invoice.VatRate / 100))
            }).ToList();

            return new InvoiceDto
            {
                header = header,
                body = body,
                payments = new List<InvoicePaymentDto>()
            };
        }
    }
}
