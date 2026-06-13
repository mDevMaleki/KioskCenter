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
            var diskId = GetWmi("Win32_DiskDrive", "SerialNumber");

            var raw = $"{cpuId}-{diskId}";

            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));

            return Convert.ToBase64String(hash);
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
