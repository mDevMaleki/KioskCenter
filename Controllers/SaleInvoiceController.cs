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
    [Authorize, RequirePermission("purchase-sale")]
    public class SaleInvoiceController : ControllerBase
    {
        private readonly CoffeeShopContext _context;
        private readonly JournalPostingService _postingService;

        public SaleInvoiceController(CoffeeShopContext context, JournalPostingService postingService)
        {
            _context = context;
            _postingService = postingService;
        }

        // GET: api/saleinvoice - لیست فاکتورهای فروش
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? partyId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var query = _context.SaleInvoices.Include(i => i.Party).AsQueryable();

            if (partyId.HasValue)
                query = query.Where(i => i.PartyId == partyId.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new
                {
                    i.Id,
                    i.PartyId,
                    PartyName = i.Party != null ? i.Party.Name : null,
                    i.TotalAmount,
                    i.Note,
                    i.CreatedAt
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        // GET: api/saleinvoice/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(int id)
        {
            var invoice = await _context.SaleInvoices
                .Include(i => i.Party)
                .Include(i => i.Items).ThenInclude(it => it.Product)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                return NotFound(new { success = false, message = "فاکتور یافت نشد" });

            return Ok(new
            {
                invoice.Id,
                invoice.PartyId,
                PartyName = invoice.Party != null ? invoice.Party.Name : null,
                invoice.TotalAmount,
                invoice.Note,
                invoice.CreatedAt,
                Items = invoice.Items!.Select(it => new
                {
                    it.Id,
                    it.ProductId,
                    ProductName = it.Product != null ? it.Product.Name : null,
                    it.Quantity,
                    it.UnitPrice,
                    it.TotalPrice
                })
            });
        }

        // POST: api/saleinvoice - ثبت فاکتور فروش
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaleInvoiceRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)
                return BadRequest(new { success = false, message = "حداقل یک قلم کالا الزامی است" });

            var party = await _context.Parties.FindAsync(request.PartyId);
            if (party == null)
                return BadRequest(new { success = false, message = "طرف حساب انتخاب‌شده معتبر نیست" });

            var invoice = new SaleInvoice
            {
                PartyId = request.PartyId,
                Note = request.Note,
                CreatedAt = DateTime.Now,
                Items = new List<SaleInvoiceItem>()
            };

            decimal totalAmount = 0;
            var stockUpdates = new List<(Product Product, decimal Quantity, decimal UnitPrice)>();
            var materialConsumption = new Dictionary<int, decimal>();

            foreach (var line in request.Items)
            {
                if (line.Quantity <= 0)
                    return BadRequest(new { success = false, message = "مقدار اقلام باید بیشتر از صفر باشد" });

                if (line.UnitPrice < 0)
                    return BadRequest(new { success = false, message = "قیمت واحد نمی‌تواند منفی باشد" });

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == line.ProductId);

                if (product == null)
                    return BadRequest(new { success = false, message = "محصول انتخاب‌شده معتبر نیست" });

                var totalPrice = line.Quantity * line.UnitPrice;

                invoice.Items.Add(new SaleInvoiceItem
                {
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    TotalPrice = totalPrice
                });

                totalAmount += totalPrice;

                product.StockQuantity -= line.Quantity;
                stockUpdates.Add((product, line.Quantity, line.UnitPrice));

                // مصرف مواد اولیه طبق فرمول محصول
                var recipeItems = await _context.ProductRecipeItems
                    .Where(r => r.ProductId == product.Id)
                    .ToListAsync();

                foreach (var recipe in recipeItems)
                {
                    var consumeQty = recipe.Quantity * line.Quantity;
                    if (materialConsumption.ContainsKey(recipe.RawMaterialId))
                        materialConsumption[recipe.RawMaterialId] += consumeQty;
                    else
                        materialConsumption[recipe.RawMaterialId] = consumeQty;
                }
            }

            invoice.TotalAmount = totalAmount;

            // افزایش طلب ما از طرف حساب (افزایش مانده)
            party.Balance += totalAmount;

            _context.SaleInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            foreach (var (product, quantity, unitPrice) in stockUpdates)
            {
                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = product.Id,
                    Type = InventoryTransactionType.Out,
                    Quantity = quantity,
                    StockAfter = product.StockQuantity,
                    UnitPrice = unitPrice,
                    Note = $"فروش طی فاکتور #{invoice.Id}",
                    CreatedAt = DateTime.Now
                });
            }

            foreach (var (rawMaterialId, consumeQty) in materialConsumption)
            {
                var material = await _context.RawMaterials.FindAsync(rawMaterialId);
                if (material == null)
                    continue;

                material.StockQuantity -= consumeQty;

                _context.RawMaterialTransactions.Add(new RawMaterialTransaction
                {
                    RawMaterialId = material.Id,
                    Type = RawMaterialTransactionType.Out,
                    Quantity = consumeQty,
                    StockAfter = material.StockQuantity,
                    Note = $"مصرف طی فاکتور فروش #{invoice.Id}",
                    CreatedAt = DateTime.Now
                });
            }

            _context.PartyTransactions.Add(new PartyTransaction
            {
                PartyId = party.Id,
                Type = PartyTransactionType.SaleInvoice,
                Amount = totalAmount,
                BalanceAfter = party.Balance,
                RefId = invoice.Id,
                Description = $"فاکتور فروش #{invoice.Id}",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            // ثبت سند حسابداری: بدهکار حساب‌های دریافتنی / بستانکار فروش محصولات
            await _postingService.PostAsync(
                invoice.CreatedAt,
                $"فاکتور فروش #{invoice.Id} - {party.Name}",
                JournalEntryRefType.SaleInvoice,
                invoice.Id,
                new List<JournalLineInput>
                {
                    new JournalLineInput { AccountId = AccountingConstants.AccountsReceivable, Debit = totalAmount, Credit = 0, PartyId = party.Id },
                    new JournalLineInput { AccountId = AccountingConstants.SalesRevenue, Debit = 0, Credit = totalAmount }
                });

            return Ok(new { success = true, message = "فاکتور فروش با موفقیت ثبت شد", invoiceId = invoice.Id, totalAmount });
        }
    }

    public class SaleInvoiceRequest
    {
        public int PartyId { get; set; }
        public string? Note { get; set; }
        public List<SaleInvoiceItemRequest> Items { get; set; } = new();
    }

    public class SaleInvoiceItemRequest
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
