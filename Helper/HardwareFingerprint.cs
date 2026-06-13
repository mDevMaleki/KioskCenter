using Microsoft.Win32;
using System.Management;
using System.Security.Cryptography;
using System.Text;

public interface IHardwareFingerprintService
{
    string ComputeHardwareHash();
    IReadOnlyDictionary<string, string> GetFingerprintParts();
}

public sealed class HardwareFingerprintService : IHardwareFingerprintService
{
    private const string Salt = "YOUR_STATIC_APP_SALT_v1";

    public string ComputeHardwareHash()
    {
        var parts = GetFingerprintParts();

        var normalized = string.Join("|", parts
            .OrderBy(x => x.Key)
            .Select(x => $"{x.Key}={Normalize(x.Value)}"));

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized + "|" + Salt));
        return Convert.ToHexString(hash);
    }

    public IReadOnlyDictionary<string, string> GetFingerprintParts()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["MachineGuid"] = GetMachineGuid(),
            ["BiosSerial"] = GetWmiProperty("Win32_BIOS", "SerialNumber"),
            ["BaseboardSerial"] = GetWmiProperty("Win32_BaseBoard", "SerialNumber"),
            ["CpuId"] = GetWmiProperty("Win32_Processor", "ProcessorId"),
            ["SystemDriveSerial"] = GetVolumeSerial("C")
        };

        var filtered = result
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .ToDictionary(x => x.Key, x => x.Value);

        if (filtered.Count < 3)
            throw new InvalidOperationException("Unable to build a stable hardware fingerprint.");

        return filtered;
    }

    private static string GetMachineGuid()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
        return key?.GetValue("MachineGuid")?.ToString() ?? string.Empty;
    }

    private static string GetWmiProperty(string className, string propertyName)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}");
            foreach (var obj in searcher.Get())
            {
                var value = obj[propertyName]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    private static string GetVolumeSerial(string driveLetter)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT VolumeSerialNumber FROM Win32_LogicalDisk WHERE DeviceID = '{driveLetter}:'");

            foreach (var obj in searcher.Get())
            {
                var value = obj["VolumeSerialNumber"]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    private static string Normalize(string input)
        => input.Trim().ToUpperInvariant();
}
