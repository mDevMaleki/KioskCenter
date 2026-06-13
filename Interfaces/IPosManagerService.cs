using KioskCenter.Models;

namespace KioskCenter.Interfaces
{
    public interface IPosManagerService
    {
        Task<List<PosDevice>> GetAllDevices();
        Task<PosDevice?> GetDevice(int id);
        Task<PosDevice> AddDevice(PosDevice device);
        Task<PosDevice> UpdateDevice(PosDevice device);
        Task<bool> DeleteDevice(int id);
        Task<PosDevice?> GetActiveDevice();
        Task<object> SendPayment(int amount, int? deviceId = null);
        Task<object> CheckConnection(int? deviceId = null);
    }
}