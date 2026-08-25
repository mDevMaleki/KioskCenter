using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KioskCenter.Authorization;
using KioskCenter.Data;
using KioskCenter.Models;
using KioskCenter.Services;

namespace KioskCenter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly CoffeeShopContext _context;
        private readonly JournalPostingService _journalPostingService;

        public OrderController(CoffeeShopContext context, JournalPostingService journalPostingService)
        {
            _context = context;
            _journalPostingService = journalPostingService;
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

        // GET: api/order/parked - فهرست سفارش‌های پارک‌شده در صندوق فروشگاهی
        [HttpGet("parked")]
        public async Task<IActionResult> GetParkedOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.IsParked)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            foreach (var order in orders)
            {
                await RecalculateOrderPrices(order);
            }

            return Ok(orders);
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
                    TaxAmount = 0,
                    Source = string.IsNullOrWhiteSpace(request.Source) ? "Kiosk" : request.Source,
                    IsParked = request.Park,
                    ParkedLabel = request.Park ? request.ParkedLabel : null
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

                    // کسر موجودی انبار به ازای فروش
                    product.StockQuantity -= item.Quantity;

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = product.Id,
                        Type = InventoryTransactionType.Out,
                        Quantity = item.Quantity,
                        StockAfter = product.StockQuantity,
                        Note = $"فروش - سفارش شماره {order.OrderNumber}",
                        CreatedAt = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();

                decimal tax = 0;
                decimal total = subtotal + tax;

                order.TotalAmount = total;
                order.TaxAmount = tax;
                order.PaymentMethodId = request.Park ? null : request.PaymentMethodId;

                await _context.SaveChangesAsync();

                // ثبت سند حسابداری بر اساس روش پرداخت (صندوق برای نقدی، بانک برای کارت/آنلاین)
                // سفارش پارک‌شده هنوز تسویه نشده، پس سندی ثبت نمی‌شود
                if (!request.Park && request.PaymentMethodId.HasValue && total > 0)
                {
                    var paymentMethod = await _context.PaymentMethods.FindAsync(request.PaymentMethodId.Value);
                    if (paymentMethod?.CashAccountId != null)
                    {
                        var cashAccount = await _context.CashAccounts.FindAsync(paymentMethod.CashAccountId.Value);
                        if (cashAccount != null)
                        {
                            await _journalPostingService.PostAsync(
                                order.OrderDate,
                                $"فروش - سفارش شماره {order.OrderNumber}",
                                JournalEntryRefType.SaleInvoice,
                                order.Id,
                                new List<JournalLineInput>
                                {
                                    new JournalLineInput { AccountId = cashAccount.AccountId, Debit = total, CashAccountId = cashAccount.Id },
                                    new JournalLineInput { AccountId = AccountingConstants.SalesRevenue, Credit = total }
                                });
                        }
                    }
                }

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
        public async Task<IActionResult> ConfirmPayment(int orderId, [FromBody] ConfirmPaymentRequest? request = null)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound(new { success = false, message = "سفارش یافت نشد" });

            if (order.PaymentStatus == "موفق")
                return BadRequest(new { success = false, message = "این سفارش قبلاً پرداخت شده است" });

            // فقط در صورتی سند حسابداری ثبت می‌شود که روش پرداخت صریحاً همراه این درخواست آمده باشد
            // و سفارش قبلاً (هنگام ایجاد) روش پرداختی نداشته باشد - تا از ثبت دوباره‌ی سند جلوگیری شود
            // (سفارش‌های عادی کیوسک از قبل روش پرداخت را هنگام ایجاد ثبت و سند را پست کرده‌اند)
            if (request?.PaymentMethodId.HasValue == true && order.PaymentMethodId == null && order.TotalAmount > 0)
            {
                var paymentMethod = await _context.PaymentMethods.FindAsync(request.PaymentMethodId!.Value);
                if (paymentMethod?.CashAccountId != null)
                {
                    var cashAccount = await _context.CashAccounts.FindAsync(paymentMethod.CashAccountId.Value);
                    if (cashAccount != null)
                    {
                        await _journalPostingService.PostAsync(
                            DateTime.Now,
                            $"فروش - سفارش شماره {order.OrderNumber}",
                            JournalEntryRefType.SaleInvoice,
                            order.Id,
                            new List<JournalLineInput>
                            {
                                new JournalLineInput { AccountId = cashAccount.AccountId, Debit = order.TotalAmount, CashAccountId = cashAccount.Id },
                                new JournalLineInput { AccountId = AccountingConstants.SalesRevenue, Credit = order.TotalAmount }
                            });
                    }
                }
                order.PaymentMethodId = request.PaymentMethodId;
            }

            order.PaymentStatus = "موفق";
            order.IsParked = false;
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

        [Authorize, RequirePermission("reports")]
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

        [Authorize, RequirePermission("reports")]
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

        [Authorize, RequirePermission("reports")]
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

        [Authorize, RequirePermission("reports")]
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

        [Authorize, RequirePermission("reports")]
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

        [Authorize, RequirePermission("reports")]
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
        public int? PaymentMethodId { get; set; }

        // ثبت به‌عنوان سفارش پارک‌شده در صندوق فروشگاهی (هنوز تسویه نشده)
        public bool Park { get; set; } = false;
        public string? ParkedLabel { get; set; }

        // منبع ثبت سفارش: Kiosk یا Cashier
        public string? Source { get; set; }
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

    public class ConfirmPaymentRequest
    {
        public int? PaymentMethodId { get; set; }
    }
}