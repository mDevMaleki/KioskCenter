// Models/OrderReportDto.cs

// Models/OrderReportDto.cs
namespace KioskCenter.Models
{
    public class OrderReportDto
    {
        public int OrderNumber { get; set; }
        public string CustomerName { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public string OrderType { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string PaymentStatus { get; set; } = "";
        public List<OrderItemReportDto> OrderItems { get; set; } = new();
    }

    public class OrderItemReportDto
    {
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}