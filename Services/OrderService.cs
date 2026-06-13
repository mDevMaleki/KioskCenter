using Microsoft.EntityFrameworkCore;
using KioskCenter.Models;
using KioskCenter.Data;
using KioskCenter.Models;

namespace KioskCenter.Services
{
    public class OrderService
    {
        private readonly CoffeeShopContext _context;

        public OrderService(CoffeeShopContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateOrderAsync(
            string customerName,
            List<(int productId, int quantity)> items,
            string orderType = "EatIn")  // پارامتر جدید
        {
            var order = new Order
            {
                OrderNumber = await GetNextOrderNumberAsync(),
                OrderDate = DateTime.Now,
                PaymentStatus = "در انتظار پرداخت",
                CustomerName = customerName,
                TotalAmount = 0,
                TaxAmount = 0,
                OrderType = orderType  // ذخیره نوع سفارش
            };
           
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            decimal subtotal = 0;

            foreach (var item in items)
            {
                var product = await _context.Products.FindAsync(item.productId);
                if (product != null)
                {
                    // تعیین قیمت بر اساس نوع سفارش
                    decimal unitPrice;
                    if (orderType == "TakeAway" && product.SecondPrice > 0)
                    {
                        unitPrice = product.SecondPrice ?? 0;  // قیمت دوم برای بیرون بر
                    }
                    else
                    {
                        unitPrice = product.Price;  // قیمت اول برای داخل سالن
                    }

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.productId,
                        Quantity = item.quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = unitPrice * item.quantity
                    };

                    _context.OrderItems.Add(orderItem);
                    subtotal += orderItem.TotalPrice;
                }
            }

            await _context.SaveChangesAsync();

          
            decimal total = subtotal;

            order.TotalAmount = total;
            order.TaxAmount = 0;
            await _context.SaveChangesAsync();

            return order;
        }

        public async Task<Order> CreateOrderWithItemsAsync(
            string customerName,
            List<OrderItemInput> items,
            string orderType = "EatIn")
        {
            var order = new Order
            {
                OrderNumber = await GetNextOrderNumberAsync(),
                OrderDate = DateTime.Now,
                PaymentStatus = "در انتظار پرداخت",
                CustomerName = customerName,
                TotalAmount = 0,
                TaxAmount = 0,
                OrderType = orderType
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            decimal subtotal = 0;

            foreach (var item in items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    decimal unitPrice;
                    if (orderType == "TakeAway" && product.SecondPrice > 0)
                    {
                        unitPrice = product.SecondPrice ?? 0;
                    }
                    else
                    {
                        unitPrice = product.Price;
                    }

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = unitPrice * item.Quantity
                    };

                    _context.OrderItems.Add(orderItem);
                    subtotal += orderItem.TotalPrice;
                }
            }

            await _context.SaveChangesAsync();

            decimal total = subtotal ;

            order.TotalAmount = total;
            order.TaxAmount = 0;
            await _context.SaveChangesAsync();

            return order;
        }

        public async Task<Order?> GetOrderDetailsAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order != null)
            {
                // محاسبه مجدد مقادیر با قیمت مناسب بر اساس نوع سفارش
                await RecalculateOrderAmounts(order);
            }

            return order;
        }

        // متد جدید برای محاسبه مجدد مبالغ سفارش
        private async Task RecalculateOrderAmounts(Order order)
        {
            decimal subtotal = 0;

            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    decimal correctUnitPrice;
                    if (order.OrderType == "TakeAway" && product.SecondPrice > 0)
                    {
                        correctUnitPrice = product.SecondPrice ?? 0;
                    }
                    else
                    {
                        correctUnitPrice = product.Price;
                    }

                    // اگر قیمت ذخیره شده با قیمت صحیح متفاوت است، به روز کن
                    if (item.UnitPrice != correctUnitPrice)
                    {
                        item.UnitPrice = correctUnitPrice;
                        item.TotalPrice = correctUnitPrice * item.Quantity;
                        _context.OrderItems.Update(item);
                    }

                    subtotal += item.TotalPrice;
                }
            }

          
            decimal total = subtotal ;

            if (order.TotalAmount != total)
            {
                order.TotalAmount = total;
                order.TaxAmount = 0;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Order?> ConfirmPaymentAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.PaymentStatus = "موفق";
                await _context.SaveChangesAsync();
            }
            return order;
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderByNumberAsync(int orderNumber)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

            if (order != null)
            {
                await RecalculateOrderAmounts(order);
            }

            return order;
        }

        private async Task<int> GetNextOrderNumberAsync()
        {
            var lastOrder = await _context.Orders
                .OrderByDescending(o => o.OrderNumber)
                .FirstOrDefaultAsync();
            return (lastOrder?.OrderNumber ?? 0) + 1;
        }
    }

    // مدل کمکی برای ورودی
    public class OrderItemInput
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}