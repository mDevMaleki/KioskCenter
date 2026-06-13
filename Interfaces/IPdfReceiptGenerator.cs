using KioskCenter.Models;

namespace KioskCenter.Interfaces
{
    public interface IPdfReceiptGenerator
    {
        string CreatePdf(Order order, int orderNumber, int invoiceNumber);
    }
}
