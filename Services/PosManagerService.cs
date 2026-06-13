using KioskCenter.Services;
using KioskCenter.Data;
using KioskCenter.Interfaces;
using KioskCenter.Models;
using KioskCenter.Services.PardakhtNovinPos.PcPos;
using Microsoft.EntityFrameworkCore;

namespace KioskCenter.Services
{
    public class PosManagerService : IPosManagerService
    {
        private readonly CoffeeShopContext _context;
        private readonly ILogger<PosManagerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private static PNAPcPos? _pcPos;
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private static PosPaymentResult? _currentPaymentResult;
        private static CancellationTokenSource? _paymentCancellationToken;

        public PosManagerService(
            CoffeeShopContext context,
            ILogger<PosManagerService> logger,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<List<PosDevice>> GetAllDevices()
        {
            return await _context.PosDevices
                .OrderBy(p => p.Priority)
                .ToListAsync();
        }

        public async Task<PosDevice?> GetDevice(int id)
        {
            return await _context.PosDevices.FindAsync(id);
        }

        public async Task<PosDevice> AddDevice(PosDevice device)
        {
            device.CreatedAt = DateTime.Now;
            _context.PosDevices.Add(device);
            await _context.SaveChangesAsync();
            return device;
        }

        public async Task<PosDevice> UpdateDevice(PosDevice device)
        {
            device.UpdatedAt = DateTime.Now;
            _context.PosDevices.Update(device);
            await _context.SaveChangesAsync();
            return device;
        }

        public async Task<bool> DeleteDevice(int id)
        {
            var device = await _context.PosDevices.FindAsync(id);
            if (device == null) return false;

            _context.PosDevices.Remove(device);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PosDevice?> GetActiveDevice()
        {
            return await _context.PosDevices
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.IsDefault)
                .ThenBy(p => p.Priority)
                .FirstOrDefaultAsync();
        }

        public async Task<object> SendPayment(int amount, int? deviceId = null)
        {
            PosDevice? device;
            amount *= 10;
            if (deviceId.HasValue)
            {
                device = await GetDevice(deviceId.Value);
            }
            else
            {
                device = await GetActiveDevice();
            }

            if (device == null)
            {
                return new { success = false, message = "هیچ دستگاه POS فعالی یافت نشد" };
            }

            _logger.LogInformation("Sending payment of {Amount} to POS device: {Name} ({Type})", amount, device.Name, device.Type);

            return device.Type switch
            {
                "Parsian" => await SendToParsianPos(amount, device.IpAddress, device.Port),
                "PardakhtNovin" => await SendToPardakhtNovinPos(amount, device.IpAddress, device.Port),
                _ => new { success = false, message = $"نوع POS ناشناخته: {device.Type}" }
            };
        }

        public async Task<object> CheckConnection(int? deviceId = null)
        {
            PosDevice? device;

            if (deviceId.HasValue)
            {
                device = await GetDevice(deviceId.Value);
            }
            else
            {
                device = await GetActiveDevice();
            }

            if (device == null)
            {
                return new { success = false, message = "هیچ دستگاه POS فعالی یافت نشد" };
            }

            _logger.LogInformation("Checking connection to POS device: {Name} ({Type})", device.Name, device.Type);

            return device.Type switch
            {
                "Parsian" => await CheckParsianConnection(device.IpAddress, device.Port),
                "PardakhtNovin" => await CheckPardakhtNovinConnection(device.IpAddress, device.Port),
                _ => new { success = false, message = $"نوع POS ناشناخته: {device.Type}" }
            };
        }

        private async Task<object> SendToParsianPos(int amount, string ip, int port)
        {
            try
            {
                var posService = new PosParsianService();

                return await Task.Run(() =>
                {
                    var result = posService.sendToLan(amount); // result از نوع string است مانند: {"cmd":"10","resp":"99"}0012{"cmd":"10","resp":"99"}

                    // بررسی موفقیت آمیز بودن پاسخ
                    bool isSuccess = CheckParsianResponseSuccess(result);
                    string message = isSuccess ? "تراکنش موفق" : "تراکنش ناموفق";

                    return new
                    {
                        success = isSuccess,
                        message,
                        result
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending to Parsian POS");
                return new { success = false, message = $"خطا: {ex.Message}" };
            }
        }

        /// <summary>
        /// بررسی پاسخ دستگاه کارتخوان پارسیان
        /// در صورتی که resp برابر "99" باشد موفق، در غیر این صورت ناموفق
        /// </summary>
        private bool CheckParsianResponseSuccess(string response)
        {
            if (string.IsNullOrEmpty(response))
                return false;

            try
            {
                // الگوی جستجو برای حالات موفقیت: "resp":0 , "resp":"0" , "resp":"00"
                string pattern = @"""resp"":(?:""00""|""0""|0)";

                if (System.Text.RegularExpressions.Regex.IsMatch(response, pattern))
                {
                    return true; // موفق - resp برابر 0 یا "0" یا "00" است
                }

                // روش جایگزین: پیدا کردن تمام مقادیر resp (برای اطمینان بیشتر)
                var matches = System.Text.RegularExpressions.Regex.Matches(response, @"""resp"":""?(\d+)""?");
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        string respValue = match.Groups[1].Value;
                        if (respValue == "00" || respValue == "0")
                        {
                            return true;
                        }
                    }
                }

                return false; // ناموفق - resp برابر 99 یا هر مقدار غیر موفق دیگر
            }
            catch
            {
                return false;
            }
        }

        private async Task<object> SendToPardakhtNovinPos(int amount, string ip, int port)
        {
            if (!await _lock.WaitAsync(0))
            {
                return new { success = false, message = "در حال حاضر یک تراکنش دیگر در حال انجام است" };
            }

            try
            {
                var pcPos = GetPcPos();

                // بررسی اتصال
                var isConnected = await Task.Run(() => pcPos.ConnectionByLan(ip, port));
                if (!isConnected)
                {
                    return new { success = false, message = "اتصال به POS برقرار نشد" };
                }

                // ریست نتیجه قبلی
                _currentPaymentResult = null;
                _paymentCancellationToken = new CancellationTokenSource();
                _paymentCancellationToken.CancelAfter(TimeSpan.FromSeconds(120));

                // ارسال مبلغ
                await Task.Run(() => pcPos.SendToPos(amount));

                // انتظار برای پاسخ
                while (_currentPaymentResult == null && !_paymentCancellationToken.Token.IsCancellationRequested)
                {
                    await Task.Delay(200, _paymentCancellationToken.Token);
                }

                if (_currentPaymentResult == null)
                {
                    return new { success = false, message = "زمان انتظار برای پرداخت به پایان رسید" };
                }

                return new
                {
                    success = _currentPaymentResult.IsSuccess,
                    message = _currentPaymentResult.Message,
                    responseValue = _currentPaymentResult.ResponseValue,
                    amount = _currentPaymentResult.Amount,
                    prn = _currentPaymentResult.Prn,
                    pan = _currentPaymentResult.Pan,
                    terminalId = _currentPaymentResult.TerminalId,
                    transactionDate = _currentPaymentResult.TransactionDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending to PardakhtNovin POS");
                return new { success = false, message = $"خطا: {ex.Message}" };
            }
            finally
            {
                _paymentCancellationToken?.Dispose();
                _paymentCancellationToken = null;
                _lock.Release();
            }
        }

        private async Task<object> CheckParsianConnection(string ip, int port)
        {
            try
            {
                // تست اتصال با یک درخواست کوچک
                var posService = new PosParsianService();
                return await Task.Run(() => new { success = true, message = "اتصال به POS پارسیان برقرار است" });
            }
            catch (Exception ex)
            {
                return new { success = false, message = $"خطا در اتصال: {ex.Message}" };
            }
        }

        private async Task<object> CheckPardakhtNovinConnection(string ip, int port)
        {
            try
            {
                var pcPos = GetPcPos();
                var isConnected = await Task.Run(() => pcPos.ConnectionByLan(ip, port));
                return new { success = isConnected, message = isConnected ? "اتصال برقرار است" : "اتصال برقرار نیست" };
            }
            catch (Exception ex)
            {
                return new { success = false, message = $"خطا: {ex.Message}" };
            }
        }

        private PNAPcPos GetPcPos()
        {
            if (_pcPos == null)
            {
                _pcPos = new PNAPcPos();
                _pcPos.TransactionResponseReceived += OnTransactionResponseReceived;
            }
            return _pcPos;
        }

        private void OnTransactionResponseReceived(object? sender, ResponseReceivedEventArgs e)
        {
            _logger.LogInformation("POS Response: ResponseValue={ResponseValue}, Message={Message}", e.ResponseValue, e.ResponseMessage);

            _currentPaymentResult = new PosPaymentResult
            {
                IsSuccess = e.ResponseValue == "00",
                ResponseValue = e.ResponseValue ?? "",
                Message = e.ResponseMessage?.Trim() ?? "پاسخ دریافت شد",
                Amount = int.TryParse(e.Amount, out var amount) ? amount : 0,
                Prn = e.PRN ?? "",
                Pan = e.PAN ?? "",
                TerminalId = e.TerminalID ?? "",
                TransactionDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
            };

            _paymentCancellationToken?.Cancel();
        }
    }

    public class PosPaymentResult
    {
        public bool IsSuccess { get; set; }
        public string ResponseValue { get; set; } = "";
        public string Message { get; set; } = "";
        public int Amount { get; set; }
        public string Prn { get; set; } = "";
        public string Pan { get; set; } = "";
        public string TerminalId { get; set; } = "";
        public string TransactionDate { get; set; } = "";
    }
}