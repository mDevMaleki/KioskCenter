using System.Text.Json.Serialization;

namespace KioskCenter.Models;
public class SignedLicense
{
    [JsonPropertyName("Version")]
    public int Version { get; set; }

    [JsonPropertyName("PayloadJson")]
    public string PayloadJson { get; set; } = "";

    // اینجا اصلاح شد: در JSON نامش Signature است ولی در کد شما SignatureBase64
    [JsonPropertyName("Signature")]
    public string SignatureBase64 { get; set; } = "";
}

public class LicensePayload
{
    public Guid LicenseId { get; set; }
    public string LicenseNumber { get; set; } = "";

    public Guid CustomerId { get; set; }
    public Guid DeviceId { get; set; }

    public string ClientId { get; set; } = "";
    public string DeviceCode { get; set; } = "";

    public string HardwareHash { get; set; } = "";

    public DateTime IssuedAtUtc { get; set; }
    public DateTime NotBeforeUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }

    public int RefreshIntervalHours { get; set; }
    public int GracePeriodHours { get; set; }

    public string[] Features { get; set; } = Array.Empty<string>();
    public int MaxUsers { get; set; }

    public string LicenseServerUrl { get; set; } = "";
    public int Revision { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
