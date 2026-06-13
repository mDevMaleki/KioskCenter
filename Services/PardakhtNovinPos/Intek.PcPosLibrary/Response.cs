using System;

namespace KioskCenter.Services.PardakhtNovinPos.Intek.PcPosLibrary;

public class Response
{
	public string RawResponse { get; set; }

	public string GetParsedResp(string response)
	{
		RawResponse = response;
		return GetParsedResp();
	}

	public string GetParsedResp()
	{
		string text = "";
		if (RawResponse != null)
		{
			text = GetTrxnResp() + "\r\n";
			text = text + GetTraceNo() + "\r\n";
			text = text + GetPANID() + "\r\n";
			text = text + GetTerminalID() + "\r\n";
			text = text + GetAmount() + "\r\n";
			text = text + GetTrxnRRN() + "\r\n";
			text = text + GetTrxnDateTime() + "\r\n";
			text = text + GetTrxnSerial() + "\r\n";
			text = text + GetBankName() + "\r\n";
			if (RawResponse.IndexOf("DS") > 0)
			{
				text = text + GetDiscount() + "\r\n";
			}
		}
		return text;
	}

	public string GetTrxnResp()
	{
		return "RS = " + RawResponse.Substring(14, 2);
	}

	public string GetTraceNo()
	{
		string text = "TR = ";
		if (RawResponse.IndexOf("TR") > 0)
		{
			text += RawResponse.Substring(RawResponse.IndexOf("TR") + 5, Convert.ToInt32(RawResponse.Substring(RawResponse.IndexOf("TR") + 2, 3)));
		}
		return text;
	}

	public string GetDiscount()
	{
		string text = "DS = ";
		if (RawResponse.IndexOf("DS") > 0)
		{
			text += RawResponse.Substring(RawResponse.IndexOf("DS") + 5, Convert.ToInt32(RawResponse.Substring(RawResponse.IndexOf("DS") + 2, 3)));
		}
		return text;
	}

	public string GetPANID()
	{
		string text = "PN = ";
		if (RawResponse.IndexOf("PN") > 0)
		{
			text += RawResponse.Substring(RawResponse.IndexOf("PN") + 5, Convert.ToInt32(RawResponse.Substring(RawResponse.IndexOf("PN") + 2, 3)));
		}
		return text;
	}

	public string GetTerminalID()
	{
		string text = "TM = ";
		if (RawResponse.IndexOf("TM") > 0)
		{
			text += RawResponse.Substring(RawResponse.IndexOf("TM") + 5, Convert.ToInt32(RawResponse.Substring(RawResponse.IndexOf("TM") + 2, 3)));
		}
		return text;
	}

	public string GetAmount()
	{
		string text = "AM = ";
		if (RawResponse.IndexOf("AM") > 0)
		{
			text += RawResponse.Substring(RawResponse.IndexOf("AM") + 5, Convert.ToInt32(RawResponse.Substring(RawResponse.IndexOf("AM") + 2, 3)));
		}
		return text;
	}

	public string GetTrxnSerial()
	{
		string text = "SR = ";
		if (RawResponse.IndexOf("SR") > 0)
		{
			text += RawResponse.Substring(RawResponse.IndexOf("SR") + 5, Convert.ToInt32(RawResponse.Substring(RawResponse.IndexOf("SR") + 2, 3)));
		}
		return text;
	}

	public string GetTrxnRRN()
	{
		string text = "RN = ";
		if (RawResponse.IndexOf("RN") > 0)
		{
			text += RawResponse.Substring(RawResponse.IndexOf("RN") + 5, Convert.ToInt32(RawResponse.Substring(RawResponse.IndexOf("RN") + 2, 3)));
		}
		return text;
	}

	public string GetTrxnDateTime()
	{
		string text = "TI = ";
		if (RawResponse.IndexOf("TI") > 0)
		{
			text += RawResponse.Substring(RawResponse.IndexOf("TI") + 5, Convert.ToInt32(RawResponse.Substring(RawResponse.IndexOf("TI") + 2, 3)));
		}
		return text;
	}

	public string GetTrxnDate()
	{
		string text = "TI = ";
		if (RawResponse.IndexOf("TI") > 0)
		{
			text += RawResponse.Substring(RawResponse.IndexOf("TI") + 5, 10);
		}
		return text;
	}

	public string GetTrxnTime()
	{
		string text = "TI = ";
		if (RawResponse.IndexOf("TI") > 0)
		{
			text += RawResponse.Substring(RawResponse.IndexOf("TI") + 5 + 10 + 1, 8);
		}
		return text;
	}

	public string GetBankName()
	{
		string result = "";
		if (RawResponse.IndexOf("PN") > 0)
		{
			result = "بانک ناشناخته";
			string text = RawResponse.Substring(RawResponse.IndexOf("PN") + 5, Convert.ToInt32(RawResponse.Substring(RawResponse.IndexOf("PN") + 2, 3)));
			if (text.Substring(0, 6) == "621986")
			{
				return "بانک سامان";
			}
			if (text.Substring(0, 6) == "622106")
			{
				return "بانک پارسیان";
			}
			if (text.Substring(0, 6) == "603799")
			{
				return "بانک ملی";
			}
			if (text.Substring(0, 6) == "589210")
			{
				return "بانک سپه";
			}
			if (text.Substring(0, 6) == "627648")
			{
				return "بانک توسعه صادرات";
			}
			if (text.Substring(0, 6) == "628023")
			{
				return "بانک مسکن";
			}
			if (text.Substring(0, 6) == "603770")
			{
				return "بانک کشاورزی";
			}
			if (text.Substring(0, 6) == "627961")
			{
				return "بانک صنعت و معدن";
			}
			if (text.Substring(0, 6) == "627760")
			{
				return " پست بانک";
			}
			if (text.Substring(0, 6) == "502908")
			{
				return "توسعه تعاون";
			}
			if (text.Substring(0, 6) == "627412")
			{
				return "اقتصاد نوین";
			}
			if (text.Substring(0, 6) == "639347")
			{
				return "بانک پاسارگاد";
			}
			if (text.Substring(0, 6) == "627488")
			{
				return "بانک کارآفرین";
			}
			if (text.Substring(0, 6) == "639346")
			{
				return "بانک سینا";
			}
			if (text.Substring(0, 6) == "639607")
			{
				return "بانک سرمایه";
			}
			if (text.Substring(0, 6) == "502806")
			{
				return "بانک شهر";
			}
			if (text.Substring(0, 6) == "603769")
			{
				return "بانک صادرات";
			}
			if (text.Substring(0, 6) == "610433")
			{
				return "بانک ملت";
			}
			if (text.Substring(0, 6) == "627353")
			{
				return "بانک تجارت";
			}
			if (text.Substring(0, 6) == "589463")
			{
				return "بانک رفاه";
			}
			if (text.Substring(0, 6) == "627381")
			{
				return "بانک انصار";
			}
			if (text.Substring(0, 6) == "502938")
			{
				return "بانک دی";
			}
			if (text.Substring(0, 6) == "505801")
			{
				return "بانک کوثر";
			}
		}
		return result;
	}

	public string GetBankBin(string respSuccess)
	{
		string text = "PN = ";
		if (RawResponse.IndexOf("PN") > 0)
		{
			string text2 = respSuccess.Substring(respSuccess.IndexOf("PN") + 5, Convert.ToInt32(respSuccess.Substring(respSuccess.IndexOf("PN") + 2, 3)));
			text += text2.Substring(0, 6);
		}
		return text;
	}
}
