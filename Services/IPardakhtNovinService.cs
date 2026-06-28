using KioskCenter.Models;

namespace KioskCenter.Services;

public interface IPardakhtNovinService
{
    event EventHandler<ResponseReceivedEventArgs> TransactionResponseReceived;
    string ResponseValue { get; set; }

	string ResponseMessage { get; set; }

	bool ConnectionByLan(string ipAddress, int portNo);

	bool SendToPos(decimal Amount, string IpAddress, int Port);
}
