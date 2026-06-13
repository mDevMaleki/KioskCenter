using KioskCenter.Data;
using KioskCenter.Services;
using KioskCenter.Services.PardakhtNovinPos.PcPos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;



namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PosController : ControllerBase
    {
        private readonly PosParsianService _posService;
        private readonly ILogger<PosController> _logger;
        private static PNAPcPos? _pcPos;
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private static PosPaymentResult? _currentPaymentResult;
        private static CancellationTokenSource? _paymentCancellationToken;

        public PosController(PosParsianService posService, ILogger<PosController> logger) 
        {
            _posService = posService;
            _logger = logger;
        }

        [HttpGet("{Amount}")]
        public async Task<IActionResult> SendAmount(decimal Amount)
        {
            var res = _posService.sendToLan(Amount);
            return Ok(res);
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
            _logger.LogInformation("POS Response: ResponseValue={ResponseValue}, Message={Message}, Amount={Amount}, PRN={PRN}",
                e.ResponseValue, e.ResponseMessage?.Trim(), e.Amount, e.PRN);

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

        /// <summary>
        /// بررسی اتصال به POS
        /// </summary>
        [HttpPost("check-connection")]
        public async Task<IActionResult> CheckConnection([FromBody] PosConnectionRequest request)
        {
            try
            {
                if (request == null)
                {
                    request = new PosConnectionRequest();
                }

                _logger.LogInformation("Checking POS connection to {Ip}:{Port}", request.Ip, request.Port);

                var pcPos = GetPcPos();

                var result = await Task.Run(() => pcPos.ConnectionByLan(request.Ip, request.Port));

                return Ok(new
                {
                    success = result,
                    message = result ? "ارتباط با POS برقرار است" : "ارتباط با POS برقرار نیست",
                    ip = request.Ip,
                    port = request.Port
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking POS connection");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"خطا: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// انجام پرداخت با POS
        /// </summary>
        [HttpPost("pay")]
        public async Task<IActionResult> StartPayment([FromBody] PosPaymentRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "درخواست نامعتبر است"
                });
            }

            // قفل برای جلوگیری از پرداخت همزمان
            if (!await _lock.WaitAsync(0))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "در حال حاضر یک تراکنش دیگر در حال انجام است. لطفاً صبر کنید."
                });
            }

            try
            {
                if (request.Amount <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "مبلغ باید بیشتر از صفر باشد"
                    });
                }

                // تنظیم مقادیر پیش‌فرض
                var ip = string.IsNullOrWhiteSpace(request.Ip) ? "192.168.1.11" : request.Ip;
                var port = request.Port == 0 ? 1362 : request.Port;

                _logger.LogInformation("Starting POS payment for amount {Amount} to {Ip}:{Port}", request.Amount, ip, port);

                var pcPos = GetPcPos();

                // 1. بررسی اتصال
                _logger.LogInformation("Checking connection...");
                var isConnected = await Task.Run(() => pcPos.ConnectionByLan(ip, port));

                if (!isConnected)
                {
                    _logger.LogWarning("Cannot connect to POS at {Ip}:{Port}", ip, port);
                    return Ok(new
                    {
                        success = false,
                        message = "اتصال به POS برقرار نشد. لطفاً از روشن بودن دستگاه و اتصال شبکه اطمینان حاصل کنید.",
                        step = "connection"
                    });
                }

                _logger.LogInformation("Connected to POS successfully");

                // 2. ریست کردن نتیجه قبلی
                _currentPaymentResult = null;
                _paymentCancellationToken = new CancellationTokenSource();

                // تنظیم timeout (مثلاً 2 دقیقه)
                _paymentCancellationToken.CancelAfter(TimeSpan.FromSeconds(120));

                // 3. ارسال مبلغ به POS
                _logger.LogInformation("Sending amount {Amount} to POS...", request.Amount);
                await Task.Run(() => pcPos.SendToPos(request.Amount));

                _logger.LogInformation("Amount sent, waiting for customer payment...");

                // 4. انتظار برای پاسخ
                while (_currentPaymentResult == null && !_paymentCancellationToken.Token.IsCancellationRequested)
                {
                    await Task.Delay(200, _paymentCancellationToken.Token);
                }

                // 5. بررسی نتیجه
                if (_currentPaymentResult == null)
                {
                    _logger.LogWarning("POS payment timeout after 120 seconds");
                    return Ok(new
                    {
                        success = false,
                        message = "زمان انتظار برای پرداخت به پایان رسید. لطفاً دوباره تلاش کنید.",
                        step = "timeout"
                    });
                }

                _logger.LogInformation("Payment completed. Success: {Success}, Message: {Message}",
                    _currentPaymentResult.IsSuccess, _currentPaymentResult.Message);

                return Ok(new
                {
                    success = _currentPaymentResult.IsSuccess,
                    message = _currentPaymentResult.Message,
                    responseValue = _currentPaymentResult.ResponseValue,
                    amount = _currentPaymentResult.Amount,
                    prn = _currentPaymentResult.Prn,
                    pan = _currentPaymentResult.Pan,
                    terminalId = _currentPaymentResult.TerminalId,
                    transactionDate = _currentPaymentResult.TransactionDate,
                    step = "complete"
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("POS payment was cancelled");
                return Ok(new
                {
                    success = false,
                    message = "عملیات پرداخت لغو شد.",
                    step = "cancelled"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during POS payment for amount {Amount}", request.Amount);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"خطا در پرداخت: {ex.Message}",
                    step = "error"
                });
            }
            finally
            {
                _paymentCancellationToken?.Dispose();
                _paymentCancellationToken = null;
                _lock.Release();
            }
        }

        /// <summary>
        /// لغو پرداخت جاری
        /// </summary>
        [HttpPost("cancel")]
        public async Task<IActionResult> CancelPayment()
        {
            try
            {
                _logger.LogInformation("Cancelling current POS payment");

                if (_paymentCancellationToken != null)
                {
                    _paymentCancellationToken.Cancel();
                    return Ok(new
                    {
                        success = true,
                        message = "درخواست لغو پرداخت ارسال شد."
                    });
                }

                return Ok(new
                {
                    success = false,
                    message = "هیچ تراکنش فعالی برای لغو وجود ندارد."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling POS payment");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"خطا در لغو پرداخت: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// دریافت وضعیت فعلی POS
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                success = true,
                isConnected = _pcPos != null,
                hasActiveTransaction = _paymentCancellationToken != null && !_paymentCancellationToken.IsCancellationRequested,
                message = "POS service is running",
                defaultIp = "192.168.1.3",
                defaultPort = 1362
            });
        }

        /// <summary>
        /// تست POS با مبلغ کم (مثلاً 1000 تومان)
        /// </summary>
        [HttpPost("test")]
        public async Task<IActionResult> TestPayment([FromBody] PosTestRequest request)
        {
            if (request == null)
            {
                request = new PosTestRequest();
            }

            var testRequest = new PosPaymentRequest
            {
                Amount = 1000,
                Ip = request.Ip,
                Port = request.Port
            };

            return await StartPayment(testRequest);
        }
    }

    // مدل‌های درخواست و پاسخ
    public class PosConnectionRequest
    {
        public string Ip { get; set; } = "192.168.1.3";
        public int Port { get; set; } = 1362;
    }

    public class PosPaymentRequest
    {
        public int Amount { get; set; }
        public string? Ip { get; set; }
        public int Port { get; set; }
    }

    public class PosTestRequest
    {
        public string? Ip { get; set; }
        public int Port { get; set; }
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

