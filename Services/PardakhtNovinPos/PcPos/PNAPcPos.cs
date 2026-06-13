using System;
using System.Linq;
using KioskCenter.Services.PardakhtNovinPos.Intek.PcPosLibrary;

namespace KioskCenter.Services.PardakhtNovinPos.PcPos;

public class PNAPcPos : IPcPos
{
	public PCPOS PcPos { get; }

	public string ResponseValue { get; set; }

	public string ResponseMessage { get; set; }

	public event EventHandler<ResponseReceivedEventArgs> TransactionResponseReceived;

	public PNAPcPos()
	{
		PcPos = new PCPOS();
		PcPos.GetResponse += PcPos_GetResponse;
	}

	protected virtual void OnTransactionResponseReceived(ResponseReceivedEventArgs e)
	{
        TransactionResponseReceived?.Invoke(this, e);
	}

	private void PcPos_GetResponse(string response)
	{
		string parsedResp = PcPos.Response.GetParsedResp(response);
		if (string.IsNullOrWhiteSpace(parsedResp))
		{
			return;
		}
		string[] source = parsedResp.Split(new string[1] { Environment.NewLine }, StringSplitOptions.None);
		string text = source.Where((c) => c.Contains("RS")).FirstOrDefault();
		if (text != null)
		{
			ResponseValue = text.Remove(0, 5).Trim();
			switch (ResponseValue)
			{
			case "12":
				ResponseMessage = "تراکنش نامعتبر است\r\n";
				break;
			case "50":
				ResponseMessage = "عدم برقراری ارتباط با مرکز\r\n";
				break;
			case "51":
				ResponseMessage = "موجودی کافی نمی باشد\r\n";
				break;
			case "54":
				ResponseMessage = "تاریخ انقضای کارت گذشده است\r\n";
				break;
			case "55":
				ResponseMessage = "رمز کارت اشتباه است\r\n";
				break;
			case "56":
				ResponseMessage = "کارت نامعتبر است\r\n";
				break;
			case "58":
				ResponseMessage = "پایانه غیر مجاز است\r\n";
				break;
			case "61":
				ResponseMessage = "مبلغ تراکنش بیش از حد مجاز می باشد\r\n";
				break;
			case "65":
				ResponseMessage = "تعداد دفعات ورود رمز غلط بیش از حد مجاز است\r\n";
				break;
			case "99":
				ResponseMessage = "لغو درخواست توسط کاربر\r\n";
				break;
			case "29":
				ResponseMessage = "مبلغ وارد شده کمتر از حد مجاز است\r\n";
				break;
			case "00":
				ResponseMessage = "تراکنش با موفقیت انجام شد\r\n";
				break;
			}
			ResponseReceivedEventArgs e = new ResponseReceivedEventArgs
			{
				Amount = PcPos.Amount,
				ResponseValue = ResponseValue,
				ResponseMessage = ResponseMessage,
				PAN = PcPos.PaymentID,
				PRN = PcPos.PrCode,
				TerminalID = PcPos.TerminalID,
				TrackingCode = string.Empty,
				TranDate = DateTime.Now.ToShortDateString()
			};
			if (ResponseValue == "00")
			{
				e.IsTransactionSuccess = true;
			}
			OnTransactionResponseReceived(e);
		}
	}

	public bool ConnectionByLan(string ipAddress, int portNo)
	{
		PcPos.ConnectionType = PCPOS.cnType.LAN;
		PcPos.Ip = ipAddress;
		PcPos.Port = portNo;
		return PcPos.TestConnection();
	}

	public bool SendToPos(int amount)
	{
		PcPos.Amount = amount.ToString();
		PcPos.send_transaction();
		return true;
	}
}
