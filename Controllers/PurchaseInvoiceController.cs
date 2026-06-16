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
    public class PurchaseInvoiceController : ControllerBase
    {
        private readonly CoffeeShopContext _context;
        private readonly JournalPostingService _postingService;

        public PurchaseInvoiceController(CoffeeShopContext context, JournalPostingService postingService)
        {
            _context = context;
            _postingService = postingService;
        }

        // GET: api/purchaseinvoice - لیست فاکتورهای خرید
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? partyId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var query = _context.PurchaseInvoices.Include(i => i.Party).AsQueryable();

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

        // GET: api/purchaseinvoice/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(int id)
        {
            var invoice = await _context.PurchaseInvoices
                .Include(i => i.Party)
                .Include(i => i.Items).ThenInclude(it => it.RawMaterial)
                .Include(i => i.Items).ThenInclude(it => it.Unit)
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
                    it.RawMaterialId,
                    RawMaterialName = it.RawMaterial != null ? it.RawMaterial.Name : null,
                    it.UnitId,
                    UnitName = it.Unit != null ? it.Unit.Name : null,
                    it.Quantity,
                    it.UnitPrice,
                    it.TotalPrice
                })
            });
        }

        // POST: api/purchaseinvoice - ثبت فاکتور خرید
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PurchaseInvoiceRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)
                return BadRequest(new { success = false, message = "حداقل یک قلم کالا الزامی است" });

            var party = await _context.Parties.FindAsync(request.PartyId);
            if (party == null)
                return BadRequest(new { success = false, message = "طرف حساب انتخاب‌شده معتبر نیست" });

            var invoice = new PurchaseInvoice
            {
                PartyId = request.PartyId,
                Note = request.Note,
                CreatedAt = DateTime.Now,
                Items = new List<PurchaseInvoiceItem>()
            };

            decimal totalAmount = 0;
            var stockUpdates = new List<(RawMaterial Material, decimal BaseQuantity, decimal UnitPrice, decimal TotalPrice)>();

            foreach (var line in request.Items)
            {
                if (line.Quantity <= 0)
                    return BadRequest(new { success = false, message = "مقدار اقلام باید بیشتر از صفر باشد" });

                if (line.UnitPrice < 0)
                    return BadRequest(new { success = false, message = "قیمت واحد نمی‌تواند منفی باشد" });

                var material = await _context.RawMaterials.FindAsync(line.RawMaterialId);
                if (material == null)
                    return BadRequest(new { success = false, message = "ماده اولیه انتخاب‌شده معتبر نیست" });

                var unit = await _context.UnitsOfMeasure.FindAsync(line.UnitId);
                if (unit == null)
                    return BadRequest(new { success = false, message = "واحد انتخاب‌شده معتبر نیست" });

                // واحد باید همان واحد پایه ماده اولیه باشد یا زیرمجموعه آن
                var isSameUnit = unit.Id == material.UnitId;
                var isDerivedUnit = unit.BaseUnitId == material.UnitId;
                if (!isSameUnit && !isDerivedUnit)
                    return BadRequest(new { success = false, message = $"واحد انتخاب‌شده با واحد ماده اولیه «{material.Name}» سازگار نیست" });

                var conversionFactor = isSameUnit ? 1 : unit.ConversionFactor;
                var baseQuantity = line.Quantity * conversionFactor;
                var totalPrice = line.Quantity * line.UnitPrice;

                invoice.Items.Add(new PurchaseInvoiceItem
                {
                    RawMaterialId = line.RawMaterialId,
                    UnitId = line.UnitId,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    TotalPrice = totalPrice
                });

                totalAmount += totalPrice;

                material.StockQuantity += baseQuantity;
                stockUpdates.Add((material, baseQuantity, line.UnitPrice, totalPrice));
            }

            invoice.TotalAmount = totalAmount;

            // افزایش بدهی ما به طرف حساب (کاهش مانده)
            party.Balance -= totalAmount;

            _context.PurchaseInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            foreach (var (material, baseQuantity, unitPrice, totalPrice) in stockUpdates)
            {
                _context.RawMaterialTransactions.Add(new RawMaterialTransaction
                {
                    RawMaterialId = material.Id,
                    Type = RawMaterialTransactionType.In,
                    Quantity = baseQuantity,
                    StockAfter = material.StockQuantity,
                    UnitPrice = unitPrice,
                    TotalPrice = totalPrice,
                    PartyName = party.Name,
                    Note = $"خرید طی فاکتور #{invoice.Id}",
                    CreatedAt = DateTime.Now
                });
            }

            _context.PartyTransactions.Add(new PartyTransaction
            {
                PartyId = party.Id,
                Type = PartyTransactionType.PurchaseInvoice,
                Amount = -totalAmount,
                BalanceAfter = party.Balance,
                RefId = invoice.Id,
                Description = $"فاکتور خرید #{invoice.Id}",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            // ثبت سند حسابداری: بدهکار موجودی مواد اولیه / بستانکار حساب‌های پرداختنی
            await _postingService.PostAsync(
                invoice.CreatedAt,
                $"فاکتور خرید #{invoice.Id} - {party.Name}",
                JournalEntryRefType.PurchaseInvoice,
                invoice.Id,
                new List<JournalLineInput>
                {
                    new JournalLineInput { AccountId = AccountingConstants.RawMaterialInventory, Debit = totalAmount, Credit = 0 },
                    new JournalLineInput { AccountId = AccountingConstants.AccountsPayable, Debit = 0, Credit = totalAmount, PartyId = party.Id }
                });

            return Ok(new { success = true, message = "فاکتور خرید با موفقیت ثبت شد", invoiceId = invoice.Id, totalAmount });
        }
    }

    public class PurchaseInvoiceRequest
    {
        public int PartyId { get; set; }
        public string? Note { get; set; }
        public List<PurchaseInvoiceItemRequest> Items { get; set; } = new();
    }

    public class PurchaseInvoiceItemRequest
    {
        public int RawMaterialId { get; set; }
        public int UnitId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
