

public class ReceiptPrintingOptions
{
    public bool Enabled { get; set; } = true;
    public string RequiredPrinterNameContains { get; set; } = "8";
    public string? PreferredPrinterName { get; set; }
    public string? SecondaryPrinterName { get; set; }
    public string? OutputDirectory { get; set; }
}
