namespace KioskCenter.Models
{
    public class PosPaymentRequest
    {
        public int Amount { get; set; }
        public string? Ip { get; set; }
        public int? Port { get; set; }
    }

    public class PosPaymentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int Amount { get; set; }
        public string Prn { get; set; } = "";
        public string Pan { get; set; } = "";
        public string TerminalId { get; set; } = "";
        public string TrackingCode { get; set; } = "";
        public string TransactionDate { get; set; } = "";
    }

    public class PosConnectionCheckRequest
    {
        public string Ip { get; set; } = "";
        public int Port { get; set; }
    }

    public class PosConnectionResponse
    {
        public bool IsConnected { get; set; }
        public string Message { get; set; } = "";
    }
}
