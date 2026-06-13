using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KioskCenter.Data;
using KioskCenter.Models;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly CoffeeShopContext _context;

        public OrderController(CoffeeShopContext context)
        {
            _context = context;
        }

        [ProducesResponseType(typeof(IEnumerable<Order>), 200)]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // محاسبه مجدد قیمت‌ها برای هر سفارش
            foreach (var order in orders)
            {
                await RecalculateOrderPrices(order);
            }

            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new { success = false, message = "سفارش یافت نشد" });

            // محاسبه مجدد قیمت‌ها
            await RecalculateOrderPrices(order);

            return Ok(order);
        }

        [HttpGet("number/{orderNumber}")]
        public async Task<IActionResult> GetByOrderNumber(int orderNumber)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

            if (order == null)
                return NotFound(new { success = false, message = "سفارش یافت نشد" });

            await RecalculateOrderPrices(order);

            return Ok(order);
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetByStatus(string status)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.PaymentStatus == status)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            foreach (var order in orders)
            {
                await RecalculateOrderPrices(order);
            }

            return Ok(orders);
        }

        [HttpGet("date/{date}")]
        public async Task<IActionResult> GetByDate(DateTime date)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.OrderDate.Date == date.Date)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            foreach (var order in orders)
            {
                await RecalculateOrderPrices(order);
            }

            return Ok(orders);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)
                return BadRequest(new { success = false, message = "حداقل یک محصول باید انتخاب شود" });

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = new Order
                {
                    OrderNumber = await GetNextOrderNumberAsync(),
                    OrderDate = DateTime.Now,
                    OrderType = request.OrderType,
                    PaymentStatus = "در انتظار پرداخت",
                    CustomerName = request.CustomerName,
                    TotalAmount = 0,
                    TaxAmount = 0
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                decimal subtotal = 0;
                var orderItems = new List<OrderItem>();

                foreach (var item in request.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                        throw new Exception($"محصول با شناسه {item.ProductId} یافت نشد");

                    // تعیین قیمت بر اساس نوع سفارش
                    decimal unitPrice;
                    if (request.OrderType == "TakeAway" && product.SecondPrice > 0)
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
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = unitPrice * item.Quantity
                    };

                    _context.OrderItems.Add(orderItem);
                    subtotal += orderItem.TotalPrice;
                    orderItems.Add(orderItem);
                }

                await _context.SaveChangesAsync();

                decimal tax = 0;
                decimal total = subtotal + tax;

                order.TotalAmount = total;
                order.TaxAmount = tax;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "سفارش با موفقیت ایجاد شد",
                    orderId = order.Id,
                    orderNumber = order.OrderNumber,
                    orderType = order.OrderType,
                    subtotal,
                    tax,
                    totalAmount = total,
                    items = orderItems.Select(oi => new
                    {
                        productId = oi.ProductId,
                        quantity = oi.Quantity,
                        unitPrice = oi.UnitPrice,
                        totalPrice = oi.TotalPrice
                    })
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderRequest request)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new { success = false, message = "سفارش یافت نشد" });

            if (!string.IsNullOrEmpty(request.CustomerName))
                order.CustomerName = request.CustomerName;

            if (!string.IsNullOrEmpty(request.PaymentStatus))
                order.PaymentStatus = request.PaymentStatus;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "سفارش با موفقیت ویرایش شد", order });
        }

        [HttpPost("confirm/{orderId}")]
        public async Task<IActionResult> ConfirmPayment(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound(new { success = false, message = "سفارش یافت نشد" });

            if (order.PaymentStatus == "موفق")
                return BadRequest(new { success = false, message = "این سفارش قبلاً پرداخت شده است" });

            order.PaymentStatus = "موفق";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "پرداخت با موفقیت انجام شد",
                orderNumber = order.OrderNumber,
                totalAmount = order.TotalAmount
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new { success = false, message = "سفارش یافت نشد" });

            if (order.PaymentStatus == "موفق")
                return BadRequest(new { success = false, message = "سفارشات پرداخت شده قابل حذف نیستند" });

            _context.OrderItems.RemoveRange(order.OrderItems);
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "سفارش با موفقیت حذف شد" });
        }

        [HttpGet("stats/summary")]
        public async Task<IActionResult> GetStats()
        {
            var totalOrders = await _context.Orders.CountAsync();
            var paidOrders = await _context.Orders.CountAsync(o => o.PaymentStatus == "موفق");
            var pendingOrders = await _context.Orders.CountAsync(o => o.PaymentStatus == "در انتظار پرداخت");
            var totalRevenue = await _context.Orders.Where(o => o.PaymentStatus == "موفق").SumAsync(o => o.TotalAmount);
            var todayRevenue = await _context.Orders
                .Where(o => o.PaymentStatus == "موفق" && o.OrderDate.Date == DateTime.Today)
                .SumAsync(o => o.TotalAmount);

            return Ok(new
            {
                totalOrders,
                paidOrders,
                pendingOrders,
                totalRevenue,
                todayRevenue,
                averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0
            });
        }

        [HttpGet("date-range")]
        public async Task<IActionResult> GetByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.OrderDate.Date >= startDate.Date && o.OrderDate.Date <= endDate.Date)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            foreach (var order in orders)
            {
                await RecalculateOrderPrices(order);
            }

            return Ok(orders);
        }

        [HttpGet("week/{year}/{week}")]
        public async Task<IActionResult> GetByWeek(int year, int week)
        {
            var startDate = GetFirstDayOfWeek(year, week);
            var endDate = startDate.AddDays(6);

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.OrderDate.Date >= startDate.Date && o.OrderDate.Date <= endDate.Date)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            foreach (var order in orders)
            {
                await RecalculateOrderPrices(order);
            }

            return Ok(orders);
        }

        [HttpGet("month/{year}/{month}")]
        public async Task<IActionResult> GetByMonth(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.OrderDate.Date >= startDate.Date && o.OrderDate.Date <= endDate.Date)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            foreach (var order in orders)
            {
                await RecalculateOrderPrices(order);
            }

            return Ok(orders);
        }

        [HttpGet("year/{year}")]
        public async Task<IActionResult> GetByYear(int year)
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.OrderDate.Date >= startDate.Date && o.OrderDate.Date <= endDate.Date)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            foreach (var order in orders)
            {
                await RecalculateOrderPrices(order);
            }

            return Ok(orders);
        }

        [HttpGet("report-stats")]
        public async Task<IActionResult> GetReportStats([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.OrderDate.Date >= startDate.Date && o.OrderDate.Date <= endDate.Date)
                .ToListAsync();

            // محاسبه مجدد قیمت‌ها
            foreach (var order in orders)
            {
                await RecalculateOrderPrices(order);
            }

            var stats = new
            {
                totalOrders = orders.Count,
                totalRevenue = orders.Where(o => o.PaymentStatus == "موفق").Sum(o => o.TotalAmount),
                cashSales = orders.Where(o => o.PaymentStatus == "موفق" && o.OrderType == "EatIn").Sum(o => o.TotalAmount),
                cardSales = orders.Where(o => o.PaymentStatus == "موفق" && o.OrderType == "TakeAway").Sum(o => o.TotalAmount),
                eatInOrders = orders.Count(o => o.OrderType == "EatIn"),
                takeAwayOrders = orders.Count(o => o.OrderType == "TakeAway"),
                averageOrderValue = orders.Count > 0 ? orders.Average(o => o.TotalAmount) : 0
            };

            return Ok(stats);
        }

        // متد کمکی برای محاسبه مجدد قیمت‌های سفارش بر اساس نوع آن
        private async Task RecalculateOrderPrices(Order order)
        {
            if (order == null || order.OrderItems == null || !order.OrderItems.Any())
                return;

            decimal subtotal = 0;
            bool needsUpdate = false;

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

                    // اگر قیمت ذخیره شده با قیمت صحیح متفاوت است
                    if (item.UnitPrice != correctUnitPrice)
                    {
                        item.UnitPrice = correctUnitPrice;
                        item.TotalPrice = correctUnitPrice * item.Quantity;
                        needsUpdate = true;
                    }

                    subtotal += item.TotalPrice;
                }
            }

            decimal tax = subtotal * 0.10m;
            decimal total = subtotal + tax;

            if (order.TotalAmount != total || order.TaxAmount != tax)
            {
                order.TotalAmount = total;
                order.TaxAmount = tax;
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
            }
        }

        private DateTime GetFirstDayOfWeek(int year, int week)
        {
            var jan1 = new DateTime(year, 1, 1);
            var daysOffset = (int)jan1.DayOfWeek - (int)DayOfWeek.Saturday;
            var firstSaturday = jan1.AddDays(-daysOffset);
            return firstSaturday.AddDays((week - 1) * 7);
        }

        private async Task<int> GetNextOrderNumberAsync()
        {
            var lastOrder = await _context.Orders
                .OrderByDescending(o => o.OrderNumber)
                .FirstOrDefaultAsync();
            return (lastOrder?.OrderNumber ?? 0) + 1;
        }
    }

    public class CreateOrderRequest
    {
        public string CustomerName { get; set; } = string.Empty;
        public string OrderType { get; set; } = "EatIn";
        public List<OrderItemRequest> Items { get; set; } = new();
    }

    public class OrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateOrderRequest
    {
        public string? CustomerName { get; set; }
        public string? PaymentStatus { get; set; }
    }
}