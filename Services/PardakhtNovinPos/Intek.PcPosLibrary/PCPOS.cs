using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace KioskCenter.Services.PardakhtNovinPos.Intek.PcPosLibrary;

public class PCPOS
{
	public delegate void ResponseEventHandler(string response);

	public enum cnType
	{
		LAN,
		SERIAL
	}

	private Dictionary<string, string> Msg_Tags;



	private TcpClient client;

	private byte[] finaMsg;

	private Thread tReader;

	public int baudRate { get; set; }

	public string Amount { get; set; }

	public string Currency { get; set; }

	public string PrCode { get; set; }

	public string R1Holder { get; set; }

	public string R3Holder { get; set; }

	public string R5Holder { get; set; }

	public string R7Holder { get; set; }

	public string R9Holder { get; set; }

	public string R2Merchant { get; set; }

	public string R4Merchant { get; set; }

	public string R6Merchant { get; set; }

	public string R8Merchant { get; set; }

	public string R0Merchant { get; set; }

	public string T1Holder { get; set; }

	public string T2Merchant { get; set; }

	public string Service { get; set; }

	public string ServiceGroup { get; set; }

	public string Settel { get; set; }

	public string KeyValue { get; set; }

	public string BIllID { get; set; }

	public string PaymentID { get; set; }

	public string TerminalID { get; set; }

	public string foodSafety { get; set; }

	public string ShiftOpen { get; set; }

	public string ShiftClose { get; set; }

	public string SignCode { get; set; }

	public string ComPort { get; set; }

	public string Ip { get; set; }

	public int Port { get; set; }

	public string Amount1 { get; set; }

	public string Amount2 { get; set; }

	public string Amount3 { get; set; }

	public string Amount4 { get; set; }

	public string Amount5 { get; set; }

	public string Amount6 { get; set; }

	public string Amount7 { get; set; }

	public string Amount8 { get; set; }

	public string Amount9 { get; set; }

	public string Amount10 { get; set; }

	public string ID1 { get; set; }

	public string ID2 { get; set; }

	public string ID3 { get; set; }

	public string ID4 { get; set; }

	public string ID5 { get; set; }

	public string ID6 { get; set; }

	public string ID7 { get; set; }

	public string ID8 { get; set; }

	public string ID9 { get; set; }

	public string ID10 { get; set; }

	public string D1 { get; set; }

	public string D2 { get; set; }

	public string D3 { get; set; }

	public string D4 { get; set; }

	public string D5 { get; set; }

	public string D6 { get; set; }

	public string D7 { get; set; }

	public string D8 { get; set; }

	public string D9 { get; set; }

	public string D10 { get; set; }

	public string Y1 { get; set; }

	public string Y2 { get; set; }

	public string Y3 { get; set; }

	public string Y4 { get; set; }

	public string Y5 { get; set; }

	public string Y6 { get; set; }

	public string Y7 { get; set; }

	public string Y8 { get; set; }

	public string Y9 { get; set; }

	public string Y10 { get; set; }

	public string CH { get; set; }

	public cnType ConnectionType { get; set; }

	public string Request { get; private set; }

	public Response Response { get; }

	public event ResponseEventHandler GetResponse;

	public PCPOS()
	{
		Msg_Tags = new Dictionary<string, string>();
		Response = new Response();
		Currency = "364";
		PrCode = "000000";
	}

	private void fillMsgParams()
	{
		Msg_Tags.Clear();
		Msg_Tags.Add("PR", PrCode);
		Msg_Tags.Add("AM", Amount);
		Msg_Tags.Add("CU", Currency);
		Msg_Tags.Add("TL", TerminalID);
		Msg_Tags.Add("SD", SignCode);
		Msg_Tags.Add("R1", R1Holder);
		Msg_Tags.Add("R2", R2Merchant);
		Msg_Tags.Add("R3", R3Holder);
		Msg_Tags.Add("R4", R4Merchant);
		Msg_Tags.Add("R5", R5Holder);
		Msg_Tags.Add("R6", R6Merchant);
		Msg_Tags.Add("R7", R7Holder);
		Msg_Tags.Add("R8", R8Merchant);
		Msg_Tags.Add("R9", R9Holder);
		Msg_Tags.Add("R0", R0Merchant);
		Msg_Tags.Add("T1", T1Holder);
		Msg_Tags.Add("T2", T2Merchant);
		Msg_Tags.Add("SV", Service);
		Msg_Tags.Add("SG", ServiceGroup);
		Msg_Tags.Add("AD", "");
		Msg_Tags.Add("A1", Amount1);
		Msg_Tags.Add("I1", ID1);
		Msg_Tags.Add("D1", D1);
		Msg_Tags.Add("Y1", Y1);
		Msg_Tags.Add("A2", Amount2);
		Msg_Tags.Add("I2", ID2);
		Msg_Tags.Add("D2", D2);
		Msg_Tags.Add("Y2", Y2);
		Msg_Tags.Add("A3", Amount3);
		Msg_Tags.Add("I3", ID3);
		Msg_Tags.Add("D3", D3);
		Msg_Tags.Add("A4", Amount4);
		Msg_Tags.Add("I4", ID4);
		Msg_Tags.Add("D4", D4);
		Msg_Tags.Add("A5", Amount5);
		Msg_Tags.Add("I5", ID5);
		Msg_Tags.Add("D5", D5);
		Msg_Tags.Add("A6", Amount6);
		Msg_Tags.Add("I6", ID6);
		Msg_Tags.Add("D6", D6);
		Msg_Tags.Add("A7", Amount7);
		Msg_Tags.Add("I7", ID7);
		Msg_Tags.Add("D7", D7);
		Msg_Tags.Add("A8", Amount8);
		Msg_Tags.Add("I8", ID8);
		Msg_Tags.Add("D8", D8);
		Msg_Tags.Add("A9", Amount9);
		Msg_Tags.Add("I9", ID9);
		Msg_Tags.Add("D9", D9);
		Msg_Tags.Add("A0", Amount10);
		Msg_Tags.Add("I0", ID10);
		Msg_Tags.Add("D0", D10);
		Msg_Tags.Add("PD", "1");
	}

	private void fillMsgParams_SL()
	{
		Msg_Tags.Clear();
		Msg_Tags.Add("PR", PrCode);
		Msg_Tags.Add("AM", Amount);
		Msg_Tags.Add("CU", Currency);
		Msg_Tags.Add("TL", TerminalID);
		Msg_Tags.Add("SD", SignCode);
		Msg_Tags.Add("R1", R1Holder);
		Msg_Tags.Add("R2", R2Merchant);
		Msg_Tags.Add("R3", R3Holder);
		Msg_Tags.Add("R4", R4Merchant);
		Msg_Tags.Add("R5", R5Holder);
		Msg_Tags.Add("R6", R6Merchant);
		Msg_Tags.Add("R7", R7Holder);
		Msg_Tags.Add("R8", R8Merchant);
		Msg_Tags.Add("R9", R9Holder);
		Msg_Tags.Add("R0", R0Merchant);
		Msg_Tags.Add("T1", T1Holder);
		Msg_Tags.Add("T2", T2Merchant);
		Msg_Tags.Add("SL", SL());
		Msg_Tags.Add("PD", "1");
	}

	private void fillMsgParams_ML()
	{
		Msg_Tags.Clear();
		Msg_Tags.Add("PR", PrCode);
		Msg_Tags.Add("AM", Amount);
		Msg_Tags.Add("CU", Currency);
		Msg_Tags.Add("TL", TerminalID);
		Msg_Tags.Add("SD", SignCode);
		Msg_Tags.Add("R1", R1Holder);
		Msg_Tags.Add("R2", R2Merchant);
		Msg_Tags.Add("R3", R3Holder);
		Msg_Tags.Add("R4", R4Merchant);
		Msg_Tags.Add("R5", R5Holder);
		Msg_Tags.Add("R6", R6Merchant);
		Msg_Tags.Add("R7", R7Holder);
		Msg_Tags.Add("R8", R8Merchant);
		Msg_Tags.Add("R9", R9Holder);
		Msg_Tags.Add("R0", R0Merchant);
		Msg_Tags.Add("T1", T1Holder);
		Msg_Tags.Add("T2", T2Merchant);
		Msg_Tags.Add("ML", ML());
		Msg_Tags.Add("PD", "1");
	}

	private string ML()
	{
		int num = 0;
		string text = "";
		if (Amount1 != null && !Amount1.Equals(""))
		{
			text = text + Amount1.ToString().PadLeft(12, '0') + D1.ToString().PadLeft(17, '0');
			num++;
		}
		if (Amount2 != null && !Amount2.Equals(""))
		{
			text = text + Amount2.ToString().PadLeft(12, '0') + D2.ToString().PadLeft(17, '0');
			num++;
		}
		if (Amount3 != null && !Amount3.Equals(""))
		{
			text = text + Amount3.ToString().PadLeft(12, '0') + D3.ToString().PadLeft(17, '0');
			num++;
		}
		if (Amount4 != null && !Amount4.Equals(""))
		{
			text = text + Amount4.ToString().PadLeft(12, '0') + D4.ToString().PadLeft(17, '0');
			num++;
		}
		if (Amount5 != null && !Amount5.Equals(""))
		{
			text = text + Amount5.ToString().PadLeft(12, '0') + D5.ToString().PadLeft(17, '0');
			num++;
		}
		if (Amount6 != null && !Amount6.Equals(""))
		{
			text = text + Amount6.ToString().PadLeft(12, '0') + D6.ToString().PadLeft(17, '0');
			num++;
		}
		if (Amount7 != null && !Amount7.Equals(""))
		{
			text = text + Amount7.ToString().PadLeft(12, '0') + D7.ToString().PadLeft(17, '0');
			num++;
		}
		if (Amount8 != null && !Amount8.Equals(""))
		{
			text = text + Amount8.ToString().PadLeft(12, '0') + D8.ToString().PadLeft(17, '0');
			num++;
		}
		if (Amount9 != null && !Amount9.Equals(""))
		{
			text = text + Amount9.ToString().PadLeft(12, '0') + D9.ToString().PadLeft(17, '0');
			num++;
		}
		if (Amount10 != null && !Amount10.Equals(""))
		{
			text = text + Amount10.ToString().PadLeft(12, '0') + D10.ToString().PadLeft(17, '0');
			num++;
		}
		text = "MLT" + num.ToString("x2") + text;
		Console.WriteLine("AAAAAAAAAA retVal : " + text);
		return text;
	}

	private string SL()
	{
		int num = 0;
		string text = "";
		if (Amount1 != null && !Amount1.Equals(""))
		{
			num = Amount1.Length;
			text = text + "ST" + (9 + num) + "AC011AM" + num.ToString().PadLeft(2, '0') + Amount1;
		}
		if (Amount2 != null && !Amount2.Equals(""))
		{
			num = Amount2.Length;
			text = text + "ST" + (9 + num) + "AC012AM" + num.ToString().PadLeft(2, '0') + Amount2;
		}
		if (Amount3 != null && !Amount3.Equals(""))
		{
			num = Amount3.Length;
			text = text + "ST" + (9 + num) + "AC013AM" + num.ToString().PadLeft(2, '0') + Amount3;
		}
		if (Amount4 != null && !Amount4.Equals(""))
		{
			num = Amount4.Length;
			text = text + "ST" + (9 + num) + "AC014AM" + num.ToString().PadLeft(2, '0') + Amount4;
		}
		if (Amount5 != null && !Amount5.Equals(""))
		{
			num = Amount5.Length;
			text = text + "ST" + (9 + num) + "AC015AM" + num.ToString().PadLeft(2, '0') + Amount5;
		}
		if (Amount6 != null && !Amount6.Equals(""))
		{
			num = Amount6.Length;
			text = text + "ST" + (9 + num) + "AC016AM" + num.ToString().PadLeft(2, '0') + Amount6;
		}
		if (Amount7 != null && !Amount7.Equals(""))
		{
			num = Amount7.Length;
			text = text + "ST" + (9 + num) + "AC017AM" + num.ToString().PadLeft(2, '0') + Amount7;
		}
		if (Amount8 != null && !Amount8.Equals(""))
		{
			num = Amount8.Length;
			text = text + "ST" + (9 + num) + "AC018AM" + num.ToString().PadLeft(2, '0') + Amount8;
		}
		if (Amount9 != null && !Amount9.Equals(""))
		{
			num = Amount9.Length;
			text = text + "ST" + (9 + num) + "AC019AM" + num.ToString().PadLeft(2, '0') + Amount9;
		}
		if (Amount10 != null && !Amount10.Equals(""))
		{
			num = Amount10.Length;
			text = text + "ST" + (10 + num) + "AC0210AM" + num.ToString().PadLeft(2, '0') + Amount10;
		}
		Console.WriteLine("AAAAAAAAAA retVal : " + text);
		return text;
	}

	private void fillMsgParamsBillPayment()
	{
		Msg_Tags.Clear();
		Msg_Tags.Add("PR", PrCode);
		Msg_Tags.Add("CU", Currency);
		Msg_Tags.Add("SD", SignCode);
		Msg_Tags.Add("R1", R1Holder);
		Msg_Tags.Add("R2", R2Merchant);
		Msg_Tags.Add("R3", R3Holder);
		Msg_Tags.Add("R4", R4Merchant);
		Msg_Tags.Add("R5", R5Holder);
		Msg_Tags.Add("R6", R6Merchant);
		Msg_Tags.Add("R7", R7Holder);
		Msg_Tags.Add("R8", R8Merchant);
		Msg_Tags.Add("R9", R9Holder);
		Msg_Tags.Add("R0", R0Merchant);
		Msg_Tags.Add("T1", T1Holder);
		Msg_Tags.Add("T2", T2Merchant);
		Msg_Tags.Add("BI", BIllID);
		Msg_Tags.Add("PI", PaymentID);
		Msg_Tags.Add("PD", "1");
	}

	private void fillMsgParamsCharge()
	{
		Msg_Tags.Clear();
		Msg_Tags.Add("PR", PrCode);
		Msg_Tags.Add("CU", Currency);
		Msg_Tags.Add("SD", SignCode);
		Msg_Tags.Add("R1", R1Holder);
		Msg_Tags.Add("R2", R2Merchant);
		Msg_Tags.Add("R3", R3Holder);
		Msg_Tags.Add("R4", R4Merchant);
		Msg_Tags.Add("R5", R5Holder);
		Msg_Tags.Add("R6", R6Merchant);
		Msg_Tags.Add("R7", R7Holder);
		Msg_Tags.Add("R8", R8Merchant);
		Msg_Tags.Add("R9", R9Holder);
		Msg_Tags.Add("R0", R0Merchant);
		Msg_Tags.Add("T1", T1Holder);
		Msg_Tags.Add("T2", T2Merchant);
		Msg_Tags.Add("CH", CH);
		Msg_Tags.Add("PD", "1");
	}

	private void fillMsgShiftOpenParams()
	{
		Msg_Tags.Clear();
		Msg_Tags.Add("SO", "1");
		Msg_Tags.Add("PD", "1");
	}

	private void fillMsgCancelTransaction()
	{
		Msg_Tags.Clear();
		Msg_Tags.Add("TC", "1");
		Msg_Tags.Add("PD", "1");
	}

	private void fillMsgAdviceTransaction()
	{
		Msg_Tags.Clear();
		Msg_Tags.Add("GA", "1");
		Msg_Tags.Add("PD", "1");
	}

	private void fillMsgReversTransaction()
	{
		Msg_Tags.Clear();
		Msg_Tags.Add("GR", "1");
		Msg_Tags.Add("PD", "1");
	}

	private void fillMsgGetLastTrxnParams()
	{
		Msg_Tags.Clear();
		Msg_Tags.Add("LT", "GetLastTrxnInfo");
		Msg_Tags.Add("PD", "1");
	}

	private void fillMsgFoodSafety()
	{
		Msg_Tags.Clear();
		Msg_Tags.Add("PR", PrCode);
		Msg_Tags.Add("AM", Amount);
		Msg_Tags.Add("CU", Currency);
		Msg_Tags.Add("SD", SignCode);
		Msg_Tags.Add("R1", R1Holder);
		Msg_Tags.Add("R2", R2Merchant);
		Msg_Tags.Add("R3", R3Holder);
		Msg_Tags.Add("R4", R4Merchant);
		Msg_Tags.Add("R5", R5Holder);
		Msg_Tags.Add("R6", R6Merchant);
		Msg_Tags.Add("R7", R7Holder);
		Msg_Tags.Add("R8", R8Merchant);
		Msg_Tags.Add("R9", R9Holder);
		Msg_Tags.Add("R0", R0Merchant);
		Msg_Tags.Add("T1", T1Holder);
		Msg_Tags.Add("T2", T2Merchant);
		Msg_Tags.Add("SV", Service);
		Msg_Tags.Add("SG", ServiceGroup);
		Msg_Tags.Add("AD", "");
		Msg_Tags.Add("FS", foodSafety);
		Msg_Tags.Add("PD", "1");
	}

	private void fillMsgShiftCloseParams()
	{
		Msg_Tags.Clear();
		Msg_Tags.Add("SC", "1");
		Msg_Tags.Add("PD", "1");
	}

	private void fillMsgGetTerminalNumber()
	{
		Msg_Tags.Clear();
		Msg_Tags.Add("GT", "1");
		Msg_Tags.Add("PD", "1");
	}

	private void fillMsgWareHoff()
	{
		Msg_Tags.Clear();
		Msg_Tags.Add("PR", PrCode);
		Msg_Tags.Add("CU", Currency);
		Msg_Tags.Add("SD", SignCode);
		Msg_Tags.Add("R1", R1Holder);
		Msg_Tags.Add("R2", R2Merchant);
		Msg_Tags.Add("R3", R3Holder);
		Msg_Tags.Add("R4", R4Merchant);
		Msg_Tags.Add("R5", R5Holder);
		Msg_Tags.Add("R6", R6Merchant);
		Msg_Tags.Add("R7", R7Holder);
		Msg_Tags.Add("R8", R8Merchant);
		Msg_Tags.Add("R9", R9Holder);
		Msg_Tags.Add("R0", R0Merchant);
		Msg_Tags.Add("T1", T1Holder);
		Msg_Tags.Add("T2", T2Merchant);
		Msg_Tags.Add("AM", Amount);
		Msg_Tags.Add("BI", BIllID);
		Msg_Tags.Add("PI", PaymentID);
		Msg_Tags.Add("PD", "1");
	}

	private byte[] BuildMsgWithExtra()
	{
		string text = "";
		foreach (KeyValuePair<string, string> msg_Tag in Msg_Tags)
		{
			if (!string.IsNullOrEmpty(msg_Tag.Value))
			{
				text = text + msg_Tag.Key.PadLeft(2, ' ') + msg_Tag.Value.Length.ToString().PadLeft(3, '0') + msg_Tag.Value;
			}
		}
		if (!string.IsNullOrEmpty(Settel))
		{
			Settel = Settel.Replace("\r\n", "\n");
			string[] array = Settel.Split(new char[1] { '\n' });
			foreach (string obj in array)
			{
				string text2 = "";
				string[] array2 = obj.Split(new char[1] { '=' });
				if (array2.Length == 2)
				{
					text2 = text2 + "AC".PadLeft(2, ' ') + array2[0].Length.ToString().PadLeft(3, '0') + array2[0];
					text2 = text2 + "AM".PadLeft(2, ' ') + array2[1].Length.ToString().PadLeft(3, '0') + array2[1];
					text = text + "ST".PadLeft(2, ' ') + text2.Length.ToString().PadLeft(3, '0') + text2;
				}
			}
		}
		if (!string.IsNullOrEmpty(KeyValue))
		{
			KeyValue = KeyValue.Replace("\r\n", "\n");
			string[] array = KeyValue.Split(new char[1] { '\n' });
			foreach (string obj2 in array)
			{
				string text3 = "";
				string[] array3 = obj2.Split(new char[1] { '=' });
				if (array3.Length == 2)
				{
					text3 = text3 + "KY".PadLeft(2, ' ') + array3[0].Length.ToString().PadLeft(3, '0') + array3[0];
					text3 = text3 + "VL".PadLeft(2, ' ') + array3[1].Length.ToString().PadLeft(3, '0') + array3[1];
					text = text + "AV".PadLeft(2, ' ') + text3.Length.ToString().PadLeft(3, '0') + text3;
					text = text + "PV".PadLeft(2, ' ') + text3.Length.ToString().PadLeft(3, '0') + text3;
				}
			}
		}
		text = Request = "RQ".PadLeft(2, ' ') + text.Length.ToString().PadLeft(3, '0') + text;
		return Encoding.GetEncoding(1256).GetBytes(text.Length.ToString().PadLeft(4, '0') + text);
	}

	private byte[] BuildMsg()
	{
		string text = "";
		foreach (KeyValuePair<string, string> msg_Tag in Msg_Tags)
		{
			if (!string.IsNullOrEmpty(msg_Tag.Value))
			{
				text = text + msg_Tag.Key.PadLeft(2, ' ') + msg_Tag.Value.Length.ToString().PadLeft(3, '0') + msg_Tag.Value;
			}
		}
		text = Request = "RQ".PadLeft(2, ' ') + text.Length.ToString().PadLeft(3, '0') + text;
		return Encoding.GetEncoding(1256).GetBytes(text.Length.ToString().PadLeft(4, '0') + text);
	}

	public void send_transaction()
	{
		fillMsgParams();
		finaMsg = BuildMsgWithExtra();
		
			sendToLan();
		
	}

	public void send_transaction_ML()
	{
		fillMsgParams_ML();
		finaMsg = BuildMsg();
			sendToLan();
		
	}

	public void send_transaction_SL()
	{
		fillMsgParams_SL();
		finaMsg = BuildMsg();
		
			sendToLan();
		
	}

	public void getTerminalNumber()
	{
		fillMsgGetTerminalNumber();
		finaMsg = BuildMsg();
		
			sendToLan();
		
	}

	public void send_transaction_WareHoff()
	{
		fillMsgWareHoff();
		finaMsg = BuildMsg();
		
			sendToLan();
		
	}

	public void send_transaction_charge()
	{
		fillMsgParamsCharge();
		finaMsg = BuildMsg();
		
			sendToLan();
		
		
	}

	public void send_transaction_bill_payment()
	{
		fillMsgParamsBillPayment();
		finaMsg = BuildMsg();
		
			sendToLan();
		
	}

	public void send_transaction_Trx_Cancel()
	{
		fillMsgCancelTransaction();
		finaMsg = BuildMsg();
		
			sendToLan();
		
	}

	public void send_transaction_Trx_Advice()
	{
		fillMsgAdviceTransaction();
		finaMsg = BuildMsg();
		
			sendToLan();
		
	}

	public void send_transaction_Trx_Revers()
	{
		fillMsgReversTransaction();
		finaMsg = BuildMsg();
		
			sendToLan();
		
	}

	public void send_transaction_Get_Lats_Trxn()
	{
		fillMsgGetLastTrxnParams();
		finaMsg = BuildMsg();
	
			sendToLan();
		
	}

	public void send_transaction_Shift_Open()
	{
		fillMsgShiftOpenParams();
		finaMsg = BuildMsg();
			sendToLan();
		
	}

	public void send_transaction_Shift_Close()
	{
		fillMsgShiftCloseParams();
		finaMsg = BuildMsg();
		
			sendToLan();
		
	}

	public void send_transaction_food_safety()
	{
		fillMsgFoodSafety();
		finaMsg = BuildMsg();
	
			sendToLan();
	
	}

	private void ReturnResponse(string request)
	{
		if (GetResponse != null)
		{
            GetResponse(request);
			return;
		}
		Response.RawResponse = "0018RS013RS00281PD0011";
		ReturnResponse(Response.RawResponse);
	}

	
	private void sendToLan()
	{
		try
		{
			if (tReader != null && tReader.IsAlive)
			{
				tReader.Abort();
			}
			if (client != null)
			{
				client.Close();
			}
			client = new TcpClient(Ip, Port);
			NetworkStream nwStream = client.GetStream();
			nwStream.Write(finaMsg, 0, finaMsg.Length);
			tReader = new Thread((ThreadStart)delegate
			{
				ReadLANResponse(nwStream);
			});
			tReader.IsBackground = true;
			tReader.Start();
		}
		catch (Exception)
		{
			Response.RawResponse = "0018RS013RS00281PD0011";
			ReturnResponse(Response.RawResponse);
			if (client != null)
			{
				client.Close();
			}
		}
	}

	private void ReadLANResponse(NetworkStream nwStream)
	{
		try
		{
			byte[] array = new byte[500];
			string text = "";
			int count;
			while ((count = nwStream.Read(array, 0, array.Length)) != 0)
			{
				text += Encoding.ASCII.GetString(array, 0, count);
				if (text.Length >= 4 && text.Length == int.Parse(text.Substring(0, 4)) + 4)
				{
					break;
				}
			}
			text = text.Substring(0, int.Parse(text.Substring(0, 4)) + 4);
			Console.WriteLine("********* ReadLANResponse: " + text);
			Response.RawResponse = text;
			ReturnResponse(Response.RawResponse);
			nwStream.Close();
			client.Close();
		}
		catch (Exception)
		{
			Response.RawResponse = "0018RS013RS00281PD0011";
			ReturnResponse(Response.RawResponse);
			if (client != null)
			{
				client.Close();
			}
		}
	}

	
	public bool TestConnection()
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		try
		{
			
				TcpClient tcpClient = new TcpClient();
				tcpClient.Connect(Ip, Port);
				if (tcpClient.Connected)
				{
					tcpClient.Close();
					return true;
				}
				return false;
		
		}
		catch (Exception)
		{
			return false;
		}
	}
}
