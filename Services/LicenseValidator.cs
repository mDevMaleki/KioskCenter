using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KioskCenter.Models;
using KioskCenter.Services;

public class LicenseValidator
{
    private readonly HardwareService _hardwareService;

    public LicenseValidator(HardwareService hardwareService)
    {
        _hardwareService = hardwareService;
    }

    public (bool IsValid, string Message, LicensePayload? Payload) Validate(string licenseFilePath)
    {
        try
        {
            if (!File.Exists(licenseFilePath))
                return (false, "License file not found.", null);

            // حذف BOM و فاصله‌های خالی احتمالی
            var json = File.ReadAllText(licenseFilePath).Trim().Trim('\uFEFF');

            // دیسریالایز کردن به مدل SignedLicense (مطمئن شوید فیلدها با فایل JSON شما همخوانی دارند)
            // فایلی که فرستادید فیلد Signature داشت، اما مدل شما احتمالاً SignatureBase64 است.
            // این بخش را بر اساس مدل خودتان چک کنید.
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var signed = JsonSerializer.Deserialize<SignedLicense>(json, options);

            if (signed == null || string.IsNullOrEmpty(signed.PayloadJson))
                return (false, "Invalid license format.", null);

            // ۱. بررسی امضا (بسیار مهم: کل آبجکت signed را بفرستید)
            if (!VerifySignature(signed))
                return (false, "Invalid digital signature.", null);

            // ۲. استخراج Payload
            var payload = JsonSerializer.Deserialize<LicensePayload>(signed.PayloadJson, options);

            if (payload == null)
                return (false, "Invalid payload content.", null);

            // ۳. بررسی سخت‌افزار
            var currentHardware = _hardwareService.GetHardwareHash();
            if (payload.HardwareHash != currentHardware)
                return (false, $"Hardware mismatch. (Expected: {payload.HardwareHash}, Current: {currentHardware})", null);

            // ۴. بررسی وضعیت ابطال
            if (payload.RevokedAtUtc != null)
                return (false, "License has been revoked.", null);

            // ۵. بررسی بازه زمانی
            var now = DateTime.UtcNow;
            if (now < payload.NotBeforeUtc)
                return (false, "License is not valid yet.", null);

            if (now > payload.ExpiresAtUtc)
                return (false, "License has expired.", null);

            return (true, "License valid.", payload);
        }
        catch (Exception ex)
        {
            return (false, $"Validation error: {ex.Message}", null);
        }
    }


    private bool VerifySignature(SignedLicense signed)
    {
        var publicKey = File.ReadAllText("Keys/public.pem");

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKey);

        var payloadBytes = Encoding.UTF8.GetBytes(signed.PayloadJson);
        var signature = Convert.FromBase64String(signed.SignatureBase64);

        return rsa.VerifyData(
            payloadBytes,
            signature,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
    }
}
