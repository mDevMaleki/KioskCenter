using System;

namespace KioskCenter.Services.PardakhtNovinPos.PcPos;

public class ResponseReceivedEventArgs : EventArgs
{
	public bool IsTransactionSuccess { get; set; }

	public string ResponseValue { get; set; }

	public string ResponseMessage { get; set; }

	public string Amount { get; set; }

	public string PRN { get; set; }

	public string PAN { get; set; }

	public string TranDate { get; set; }

	public string TerminalID { get; set; }

	public string TrackingCode { get; set; }
}
