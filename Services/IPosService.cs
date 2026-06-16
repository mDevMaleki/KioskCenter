namespace KioskCenter.Services
{
    public interface IPosService
    {
        Task<string> sendToLan(decimal Amount, string IpAddress, int Port, PosType posType);
    }
}
