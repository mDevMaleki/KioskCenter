using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using KioskCenter.Models;

namespace KioskCenter.Services;

public class PardakhtNovinService : IPardakhtNovinService
{
    public delegate void ResponseEventHandler(string response);

    private readonly Dictionary<string, string> Msg_Tags;

    private TcpClient client;
    private byte[] finaMsg;
    private Thread tReader;

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

    public string PaymentID { get; set; }
    public string TerminalID { get; set; }

    public string SignCode { get; set; }

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

    public string Request { get; private set; }

    public ResponsePardakhtNovinPos Response { get; }

    public event ResponseEventHandler GetResponse;

    public string ResponseValue { get; set; }

    public string ResponseMessage { get; set; }

    public event EventHandler<ResponseReceivedEventArgs> TransactionResponseReceived;

    public PardakhtNovinService()
    {
        Msg_Tags = new Dictionary<string, string>();
        Response = new ResponsePardakhtNovinPos();

        Currency = "364";
        PrCode = "000000";

        GetResponse += PcPos_GetResponse;
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

    private byte[] BuildMsgWithExtra()
    {
        string text = "";

        foreach (KeyValuePair<string, string> msg_Tag in Msg_Tags)
        {
            if (!string.IsNullOrEmpty(msg_Tag.Value))
            {
                text += msg_Tag.Key.PadLeft(2, ' ')
                     + msg_Tag.Value.Length.ToString().PadLeft(3, '0')
                     + msg_Tag.Value;
            }
        }

        if (!string.IsNullOrEmpty(Settel))
        {
            Settel = Settel.Replace("\r\n", "\n");
            string[] array = Settel.Split('\n');

            foreach (string obj in array)
            {
                string text2 = "";
                string[] array2 = obj.Split('=');

                if (array2.Length == 2)
                {
                    text2 += "AC".PadLeft(2, ' ')
                          + array2[0].Length.ToString().PadLeft(3, '0')
                          + array2[0];

                    text2 += "AM".PadLeft(2, ' ')
                          + array2[1].Length.ToString().PadLeft(3, '0')
                          + array2[1];

                    text += "ST".PadLeft(2, ' ')
                         + text2.Length.ToString().PadLeft(3, '0')
                         + text2;
                }
            }
        }

        if (!string.IsNullOrEmpty(KeyValue))
        {
            KeyValue = KeyValue.Replace("\r\n", "\n");
            string[] array = KeyValue.Split('\n');

            foreach (string obj2 in array)
            {
                string text3 = "";
                string[] array3 = obj2.Split('=');

                if (array3.Length == 2)
                {
                    text3 += "KY".PadLeft(2, ' ')
                          + array3[0].Length.ToString().PadLeft(3, '0')
                          + array3[0];

                    text3 += "VL".PadLeft(2, ' ')
                          + array3[1].Length.ToString().PadLeft(3, '0')
                          + array3[1];

                    text += "AV".PadLeft(2, ' ')
                         + text3.Length.ToString().PadLeft(3, '0')
                         + text3;

                    text += "PV".PadLeft(2, ' ')
                         + text3.Length.ToString().PadLeft(3, '0')
                         + text3;
                }
            }
        }

        text = Request = "RQ".PadLeft(2, ' ')
                       + text.Length.ToString().PadLeft(3, '0')
                       + text;

        string finalText = text.Length.ToString().PadLeft(4, '0') + text;

        return Encoding.GetEncoding(1256).GetBytes(finalText);
    }

    public void send_transaction(string IpAddress, int Port)
    {
        fillMsgParams();

        finaMsg = BuildMsgWithExtra();

        sendToLan(IpAddress, Port);
    }

    private void ReturnResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            response = "0018RS013RS00281PD0011";
        }

        Response.RawResponse = response;

        GetResponse?.Invoke(response);
    }

    private void sendToLan(string IpAddress, int Port)
    {
        try
        {
            try
            {
                if (client != null)
                {
                    client.Close();
                    client.Dispose();
                    client = null;
                }
            }
            catch
            {
                // Ignore dispose errors
            }

            client = new TcpClient();

            client.ReceiveTimeout = 120000;
            client.SendTimeout = 30000;

            client.Connect(IpAddress, Port);

            NetworkStream nwStream = client.GetStream();

            nwStream.Write(finaMsg, 0, finaMsg.Length);
            nwStream.Flush();

            tReader = new Thread(() =>
            {
                ReadLANResponse(nwStream);
            });

            tReader.IsBackground = true;
            tReader.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine("POS SEND ERROR: " + ex.Message);

            ReturnResponse("0018RS013RS00281PD0011");

            try
            {
                client?.Close();
            }
            catch
            {
                // Ignore close errors
            }
        }
    }

    private void ReadLANResponse(NetworkStream nwStream)
    {
        try
        {
            byte[] buffer = new byte[1024];
            StringBuilder response = new StringBuilder();

            int expectedLength = -1;

            while (true)
            {
                int bytesRead = nwStream.Read(buffer, 0, buffer.Length);

                if (bytesRead <= 0)
                {
                    break;
                }

                string part = Encoding.GetEncoding(1256).GetString(buffer, 0, bytesRead);

                response.Append(part);

                string current = response.ToString();

                if (current.Length >= 4 && expectedLength == -1)
                {
                    string lenText = current.Substring(0, 4);

                    if (!int.TryParse(lenText, out int bodyLen))
                    {
                        throw new Exception("Invalid POS response length: " + lenText);
                    }

                    expectedLength = bodyLen + 4;
                }

                if (expectedLength > 0 && current.Length >= expectedLength)
                {
                    string finalResponse = current.Substring(0, expectedLength);

                    Console.WriteLine("POS RESPONSE = " + finalResponse);

                    ReturnResponse(finalResponse);

                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("POS READ ERROR: " + ex.Message);

            ReturnResponse("0018RS013RS00281PD0011");
        }
        finally
        {
            try
            {
                nwStream?.Close();
            }
            catch
            {
                // Ignore close errors
            }

            try
            {
                client?.Close();
            }
            catch
            {
                // Ignore close errors
            }
        }
    }

    public bool TestConnection(string IpAddress, int Port)
    {
        try
        {
            using TcpClient tcpClient = new TcpClient();

            tcpClient.ReceiveTimeout = 5000;
            tcpClient.SendTimeout = 5000;

            IAsyncResult result = tcpClient.BeginConnect(IpAddress, Port, null, null);

            bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));

            if (!success)
            {
                return false;
            }

            tcpClient.EndConnect(result);

            return tcpClient.Connected;
        }
        catch
        {
            return false;
        }
    }

    protected virtual void OnTransactionResponseReceived(ResponseReceivedEventArgs e)
    {
        TransactionResponseReceived?.Invoke(this, e);
    }

    
    private Dictionary<string, string> ParseTlvFields(string response)
    {
        Dictionary<string, string> fields = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(response))
            return fields;

        string body = response;

        if (body.Length >= 4 && int.TryParse(body.Substring(0, 4), out _))
        {
            body = body.Substring(4);
        }

        ParseTlvRecursive(body, fields);

        return fields;
    }

    private bool LooksLikeTlv(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.Length < 5)
            return false;

        string lenText = value.Substring(2, 3);

        return int.TryParse(lenText, out int len) && value.Length >= 5 + len;
    }


    private void ParseTlvRecursive(string text, Dictionary<string, string> fields)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        int index = 0;

        while (index + 5 <= text.Length)
        {
            string tag = text.Substring(index, 2);
            string lenText = text.Substring(index + 2, 3);

            if (!int.TryParse(lenText, out int len))
                break;

            int valueStart = index + 5;

            if (len < 0 || valueStart + len > text.Length)
                break;

            string value = text.Substring(valueStart, len);

            fields[tag] = value;

            if (LooksLikeTlv(value))
            {
                ParseTlvRecursive(value, fields);
            }

            index = valueStart + len;
        }
    }

    private void PcPos_GetResponse(string response)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(response))
                return;

            Console.WriteLine("RAW POS RESPONSE = " + response);

            Dictionary<string, string> fields = ParseTlvFields(response);

            fields.TryGetValue("RS", out string rs);
            fields.TryGetValue("TR", out string tracking);
            fields.TryGetValue("PN", out string pan);
            fields.TryGetValue("TI", out string terminalId);

            ResponseValue = rs;

            Console.WriteLine("RS = " + ResponseValue);
            Console.WriteLine("TR = " + tracking);

            if (string.IsNullOrWhiteSpace(ResponseValue))
            {
                ResponseValue = "81";
                ResponseMessage = "خطا در پردازش پاسخ دستگاه کارتخوان";
            }
            else
            {
                ResponseMessage = GetResponseMessage(ResponseValue);
            }

            ResponseReceivedEventArgs e = new ResponseReceivedEventArgs
            {
                Amount = Amount,
                ResponseValue = ResponseValue,
                ResponseMessage = ResponseMessage,
                PAN = string.IsNullOrWhiteSpace(pan) ? PaymentID : pan,
                PRN = PrCode,
                TerminalID = string.IsNullOrWhiteSpace(terminalId) ? TerminalID : terminalId,
                TrackingCode = tracking ?? "",
                TranDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                IsTransactionSuccess = ResponseValue == "00"
            };
            Console.WriteLine("BEFORE EVENT");
            OnTransactionResponseReceived(e);
            Console.WriteLine("AFTER EVENT");
        }
        catch (Exception ex)
        {
            Console.WriteLine("POS PARSE ERROR: " + ex.Message);

            ResponseValue = "81";
            ResponseMessage = "خطا در دریافت یا پردازش پاسخ دستگاه کارتخوان";

            ResponseReceivedEventArgs e = new ResponseReceivedEventArgs
            {
                Amount = Amount,
                ResponseValue = ResponseValue,
                ResponseMessage = ResponseMessage,
                PAN = PaymentID,
                PRN = PrCode,
                TerminalID = TerminalID,
                TrackingCode = "",
                TranDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                IsTransactionSuccess = false
            };

            OnTransactionResponseReceived(e);
        }
    }


    private string GetResponseMessage(string responseCode)
    {
        return responseCode switch
        {
            "00" => "تراکنش با موفقیت انجام شد",
            "12" => "تراکنش نامعتبر است",
            "29" => "مبلغ وارد شده کمتر از حد مجاز است",
            "50" => "عدم برقراری ارتباط با مرکز",
            "51" => "موجودی کافی نمی باشد",
            "54" => "تاریخ انقضای کارت گذشته است",
            "55" => "رمز کارت اشتباه است",
            "56" => "کارت نامعتبر است",
            "58" => "پایانه غیر مجاز است",
            "61" => "مبلغ تراکنش بیش از حد مجاز می باشد",
            "65" => "تعداد دفعات ورود رمز غلط بیش از حد مجاز است",
            "81" => "خطا در دریافت یا پردازش پاسخ دستگاه کارتخوان",
            "99" => "لغو درخواست توسط کاربر",
            _ => "خطای نامشخص: " + responseCode
        };
    }

    public bool ConnectionByLan(string ipAddress, int portNo)
    {
        return TestConnection(ipAddress, portNo);
    }

    public bool SendToPos(decimal Amount, string IpAddress, int Port)
    {
        this.Amount = ((long)Amount).ToString();

        send_transaction(IpAddress, Port);

        return true;
    }


  

}
