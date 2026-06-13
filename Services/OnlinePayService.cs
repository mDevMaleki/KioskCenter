using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;

namespace KioskCenter.Services
{
    public class OnlinePayService
    {
        // استفاده از Merchant ID واقعی شما
        private readonly string _merchantId = "1bd792f2-d2bf-4811-90c3-9ff6009137c3";
        private readonly HttpClient _httpClient;

        public OnlinePayService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<byte[]> CreatePaymentQr(decimal amount)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://payment.zarinpal.com/pg/v4/payment/request.json");
                request.Headers.Add("accept", "application/json");

                var json = $@"{{
                  ""merchant_id"": ""{_merchantId}"",
                  ""amount"": {amount},
                  ""callback_url"": ""https://fujicctv.ir/verify"",
                  ""description"": ""پرداخت سفارش کیوسک""
                }}";

                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var result = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Zarinpal Response: {result}");

                var doc = JsonDocument.Parse(result);

                // بررسی موفقیت آمیز بودن درخواست
                if (doc.RootElement.TryGetProperty("data", out var data))
                {
                    var authority = data.GetProperty("authority").GetString();
                    var paymentUrl = $"https://payment.zarinpal.com/pg/StartPay/{authority}";

                    // ساخت QR
                    using var qrGenerator = new QRCodeGenerator();
                    var qrData = qrGenerator.CreateQrCode(paymentUrl, QRCodeGenerator.ECCLevel.Q);
                    var qrCode = new QRCode(qrData);

                    using Bitmap qrImage = qrCode.GetGraphic(20);

                    using var ms = new MemoryStream();
                    qrImage.Save(ms, ImageFormat.Png);

                    return ms.ToArray();
                }

                throw new Exception("خطا در دریافت authority از زرین‌پال");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreatePaymentQr: {ex.Message}");
                throw;
            }
        }
        public async Task<string> CreatePaymentAndGetAuthority(decimal amount)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://payment.zarinpal.com/pg/v4/payment/request.json");
            request.Headers.Add("accept", "application/json");

            var json = $@"{{
      ""merchant_id"": ""{_merchantId}"",
      ""amount"": {amount},
      ""callback_url"": ""https://fujicctv.ir/verify"",
      ""description"": ""پرداخت سفارش کیوسک""
    }}";

            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            var doc = JsonDocument.Parse(result);
            var authority = doc.RootElement
                .GetProperty("data")
                .GetProperty("authority")
                .GetString();

            return authority;
        }
        public async Task<PaymentVerificationResult> VerifyPayment(decimal amount, string authority)
        {
            var result = new PaymentVerificationResult();

            try
            {
                using var client = new HttpClient();

                var json = $@"{{
            ""merchant_id"": ""{_merchantId}"",
            ""amount"": {amount},
            ""authority"": ""{authority}""
        }}";

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://payment.zarinpal.com/pg/v4/payment/verify.json", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Verify Response: {responseBody}");

                var jsonDoc = JsonDocument.Parse(responseBody);

                // اول خطاها رو بررسی کن
                if (jsonDoc.RootElement.TryGetProperty("errors", out var errors))
                {
                    if (errors.TryGetProperty("code", out var codeProp))
                    {
                        result.Code = codeProp.GetInt32();
                    }
                    if (errors.TryGetProperty("message", out var msgProp))
                    {
                        result.Message = msgProp.GetString();
                    }
                    result.IsSuccess = false;
                }
                // بعد دیتا رو بررسی کن
                else if (jsonDoc.RootElement.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty("code", out var codeProp))
                    {
                        result.Code = codeProp.GetInt32();
                        result.IsSuccess = result.Code == 100;
                    }

                    if (data.TryGetProperty("message", out var msgProp))
                    {
                        result.Message = msgProp.GetString();
                    }

                    if (result.IsSuccess)
                    {
                        if (data.TryGetProperty("ref_id", out var refProp))
                        {
                            result.RefId = refProp.GetString();
                        }
                        if (data.TryGetProperty("card_pan", out var cardProp))
                        {
                            result.CardPan = cardProp.GetString();
                        }
                    }
                }
                else
                {
                    result.IsSuccess = false;
                    result.Code = -1;
                    result.Message = "پاسخ نامعتبر از زرین‌پال";
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Code = -1;
                result.Message = ex.Message;
            }

            return result;
        }
    }
        public class PaymentVerificationResult
    {
        public bool IsSuccess { get; set; }
        public int Code { get; set; }
        public string Message { get; set; } = "";
        public string RefId { get; set; } = "";
        public string CardPan { get; set; } = "";
    }
}