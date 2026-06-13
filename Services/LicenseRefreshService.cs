using System.Net.Http.Json;
using System.Text.Json;
using KioskCenter.Models;

public class LicenseRefreshService
{
    private readonly HttpClient _httpClient;

    public LicenseRefreshService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> RefreshAsync(LicensePayload payload)
    {
        try
        {
            // ۱. بر اساس تصویر Swagger شما، آدرس دانلود لایسنس جدید این است:
            // GET /api/licenses/{id}/download

            // نکته: اگر در دیتابیس ID لایسنس GUID است، از Payload.LicenseId استفاده کنید
            // اگر ID همان لایسنس نامبر است، از Payload.LicenseNumber استفاده کنید.
            Guid licenseIdForUrl = payload.LicenseId;

            string downloadUrl = $"{payload.LicenseServerUrl.TrimEnd('/')}/api/licenses/{licenseIdForUrl}/download";

            Console.WriteLine($"Refreshing license from: {downloadUrl}");

            var response = await _httpClient.GetAsync(downloadUrl);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to refresh. Server returned: {response.StatusCode}");
                return false;
            }

            // ۲. سرور کل فایل SignedLicense (شامل امضا و پلود) را برمی‌گرداند
            var latestLicenseJson = await response.Content.ReadAsStringAsync();

            // ۳. ذخیره روی فایل قبلی
            await File.WriteAllTextAsync("license.dat", latestLicenseJson);

            Console.WriteLine("License updated successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during refresh: {ex.Message}");
            return false;
        }
    }
}

