namespace KioskCenter.Services.PardakhtNovinPos.PcPos;

internal interface IPcPos
{
	string ResponseValue { get; set; }

	string ResponseMessage { get; set; }

	bool ConnectionByLan(string ipAddress, int portNo);

	bool SendToPos(int amount);
}
