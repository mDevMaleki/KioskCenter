using System.ComponentModel;
using System.Text.Json;
using KioskCenter.Models;

public class LicenseManager
{
    private const string LicensePath = "license.dat";
    private const string StatePath = "license_state.json";

    private readonly LicenseValidator _validator;

    public LicenseManager(LicenseValidator validator)
    {
        _validator = validator;
    }


    private async Task<bool> CheckServerStatus(Guid licenseId, string serverUrl)
    {
        try
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(5);

            // لاگ کنید که چه آدرسی ساخته می‌شود
            string fullUrl = $"{serverUrl.TrimEnd('/')}/api/licenses/{licenseId}/status";
            Console.WriteLine($"🔍 Requesting: {fullUrl}");

            var response = await http.GetAsync(fullUrl);

            Console.WriteLine($"📡 Server Response: {response.StatusCode}");

            if (!response.IsSuccessStatusCode) return false;

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"📄 Content: {content}");

            return content.Contains("\"Status\":\"Active\"", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Connection Error: {ex.Message}");
            return false;
        }
    }



    private LicensePayload ReadPayload()
    {
        var json = File.ReadAllText(LicensePath);

        var signed = JsonSerializer.Deserialize<SignedLicense>(json)!;

        var payload = JsonSerializer.Deserialize<LicensePayload>(signed.PayloadJson)!;

        return payload;
    }


    public async Task<bool> ValidateOnStartup()
    {
        var result = _validator.Validate(LicensePath);

        if (!result.IsValid)
        {
            Console.WriteLine(result.Message);
            return false;
        }

        var payload = ReadPayload();

        var ok = await CheckServerStatus(
            payload.LicenseId,
            payload.LicenseServerUrl);

        if (!ok)
        {
            Console.WriteLine("License revoked by server");
            return false;
        }

        Console.WriteLine("License OK ✅");
        return true;
    }


    public void SaveLastRefresh()
    {
        var state = new { LastRefreshUtc = DateTime.UtcNow };
        File.WriteAllText(StatePath, JsonSerializer.Serialize(state));
    }

    public DateTime? GetLastRefresh()
    {
        if (!File.Exists(StatePath))
            return null;

        var json = File.ReadAllText(StatePath);
        var doc = JsonDocument.Parse(json);

        return doc.RootElement.GetProperty("LastRefreshUtc").GetDateTime();
    }
}
