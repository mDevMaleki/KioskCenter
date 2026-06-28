namespace KioskCenter.Services
{
    using System.Management;
    using System.Security.Cryptography;
    using System.Text;

    public class HardwareService
    {
        public string GetHardwareHash()
        {
            var cpuId = GetWmi("Win32_Processor", "ProcessorId");
            var diskId = GetSystemDiskSerial();

            var raw = $"{cpuId}-{diskId}";

            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));

            return Convert.ToBase64String(hash);
        }

        // سریال دیسک فیزیکی شماره ۰ (دیسک بوت سیستم) به جای اولین دیسکی که WMI برمی‌گرداند
        // تا با وصل/جدا شدن دیسک‌های دیگر (فلش، هارد اکسترنال و ...) هش تغییر نکند.
        // از کوئری‌های ASSOCIATORS استفاده نمی‌کنیم چون روی بعضی سیستم‌ها بسیار کند/معلق می‌شوند.
        private string GetSystemDiskSerial()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT SerialNumber FROM Win32_DiskDrive WHERE Index = 0");

                foreach (ManagementObject disk in searcher.Get())
                {
                    var serial = disk["SerialNumber"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(serial))
                        return serial;
                }
            }
            catch
            {
                // در صورت بروز خطا، به روش قبلی (اولین دیسک) بازگردیم
            }

            return GetWmi("Win32_DiskDrive", "SerialNumber");
        }

        private string GetWmi(string className, string property)
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {className}");

            foreach (ManagementObject obj in searcher.Get())
            {
                return obj[property]?.ToString()?.Trim() ?? "";
            }

            return "";
        }
    }

}
