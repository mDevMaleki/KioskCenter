using KioskCenter.Models;

namespace KioskCenter.Interfaces
{
    public interface IReceiptPrinter
    {
        bool TryPrint(Order order, int orderNumber, int invoiceNumber);
        bool PrintReport(byte[] pdfBytes);
        bool TestPrint(string printerName);
        bool PrintToSpecificPrinter(byte[] pdfBytes, string printerName); // اضافه کردن این خط
    }
}