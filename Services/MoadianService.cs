using KioskCenter.Data;
using KioskCenter.Models;
using Microsoft.EntityFrameworkCore;
using TaxCollectData.Library.Abstraction.Clients;
using TaxCollectData.Library.Abstraction.Cryptography;
using TaxCollectData.Library.Algorithms;
using TaxCollectData.Library.Dto;
using TaxCollectData.Library.Factories;
using TaxCollectData.Library.Properties;
using TaxCollectData.Library.Providers;

namespace KioskCenter.Services
{
    // سرویس اتصال به سامانه مودیان با استفاده از SDK رسمی سازمان امور مالیاتی (TaxCollectData.Library)
    // این سرویس فقط در صورتی فعال می‌شود که کاربر در تنظیمات صریحاً IsEnabled را تیک بزند
    public class MoadianService
    {
        private readonly CoffeeShopContext _context;
        private readonly IWebHostEnvironment _env;

        public MoadianService(CoffeeShopContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private string PrivateKeyFilePath => Path.Combine(_env.ContentRootPath, "Keys", "moadian_private_key.pem");
        private string CertificateFilePath => Path.Combine(_env.ContentRootPath, "Keys", "moadian_certificate.cer");

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

        // ذخیره کلید خصوصی و گواهی روی دیسک (فقط هنگام تغییر تنظیمات) - SDK این مقادیر را به‌صورت مسیر فایل می‌خواهد
        public void PersistKeyFiles(string? privateKeyPem, string? certificatePem)
        {
            Directory.CreateDirectory(Path.Combine(_env.ContentRootPath, "Keys"));

            if (!string.IsNullOrWhiteSpace(privateKeyPem))
                File.WriteAllText(PrivateKeyFilePath, privateKeyPem);

            if (!string.IsNullOrWhiteSpace(certificatePem))
                File.WriteAllText(CertificateFilePath, certificatePem);
        }

        private ITaxApi BuildTaxApi(MoadianSetting setting)
        {
            var pkcs8SignatoryFactory = new Pkcs8SignatoryFactory();
            var properties = new TaxProperties(setting.MemoryId!);
            var encryptorFactory = new EncryptorFactory();
            var taxApiFactory = new TaxApiFactory(setting.ApiUrl, properties);

            ISignatory signatory = pkcs8SignatoryFactory.Create(PrivateKeyFilePath, CertificateFilePath);
            ITaxPublicApi publicApi = taxApiFactory.CreatePublicApi(signatory);
            IEncryptor encryptor = encryptorFactory.Create(publicApi);

            return taxApiFactory.CreateApi(signatory, encryptor);
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
                if (string.IsNullOrWhiteSpace(setting.MemoryId)
                    || string.IsNullOrWhiteSpace(setting.SellerEconomicCode)
                    || !File.Exists(PrivateKeyFilePath)
                    || !File.Exists(CertificateFilePath))
                {
                    invoice.MoadianError = "تنظیمات اتصال (شناسه حافظه/شماره اقتصادی/کلیدها) کامل نیست";
                    await _context.SaveChangesAsync();
                    return false;
                }

                var taxApi = BuildTaxApi(setting);

                var taxIdProvider = new TaxIdProvider(new VerhoeffAlgorithm());
                var serial = GenerateSerial(invoice.Id);
                var taxId = taxIdProvider.GenerateTaxId(setting.MemoryId!, serial, invoice.CreatedAt);
                var inno = serial.ToString("X").PadLeft(10, '0');
                var indatim = new DateTimeOffset(invoice.CreatedAt).ToUnixTimeMilliseconds();

                var invoiceDto = BuildInvoiceDto(invoice, taxId, inno, indatim, setting);

                var responseModels = taxApi.SendInvoices(new List<InvoiceDto> { invoiceDto });
                var response = responseModels.FirstOrDefault();

                invoice.MoadianTaxId = taxId;
                invoice.MoadianSent = true;
                invoice.MoadianSentAt = DateTime.Now;
                invoice.MoadianReferenceNumber = response?.ReferenceNumber;
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

        // تولید یک شماره سریال غیرتکراری برای هر فاکتور (استفاده در ساخت شماره مالیاتی و inno)
        private static long GenerateSerial(int invoiceId)
        {
            // ترکیب شناسه فاکتور با timestamp برای یکتایی در عین قابلیت ردیابی
            var ticks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1_000_000_000L;
            return (long)invoiceId * 1_000_000_000L + ticks % 1_000_000_000L;
        }

        private InvoiceDto BuildInvoiceDto(SaleInvoice invoice, string taxId, string inno, long indatim, MoadianSetting setting)
        {
            var header = new HeaderDto
            {
                taxid = taxId,
                indatim = indatim,
                inty = 1,  // نوع فاکتور: فروش کالا/خدمات (طبق دستورالعمل سازمان امور مالیاتی تنظیم شود)
                inno = inno,
                inp = 1,   // الگوی فاکتور: عادی
                ins = 1,   // موضوع فاکتور: فروش کالا
                tins = setting.SellerEconomicCode,
                tob = 1,   // نوع خریدار: حقیقی (در صورت نیاز اصلاح شود)
                bid = invoice.PartyId.ToString(),
                tinb = invoice.Party?.EconomicCode,
                tprdis = 0L,
                tdis = 0L,
                tadis = (long)invoice.TotalAmount,
                tvam = (long)invoice.VatAmount,
                todam = 0L,
                tbill = (long)invoice.GrandTotal,
                setm = 1
            };

            var body = invoice.Items!.Select(item =>
            {
                var vatAmount = Math.Round(item.TotalPrice * invoice.VatRate / 100, 2);
                return new BodyItemDto
                {
                    sstt = item.Product?.Name ?? "",
                    mu = "عدد",
                    am = item.Quantity,
                    fee = item.UnitPrice,
                    prdis = 0L,
                    dis = 0L,
                    adis = (long)item.TotalPrice,
                    vra = invoice.VatRate,
                    vam = (long)vatAmount,
                    tsstam = (long)(item.TotalPrice + vatAmount)
                };
            }).ToList();

            return new InvoiceDto
            {
                Header = header,
                Body = body,
                Payments = new List<PaymentItemDto>()
            };
        }
    }
}
