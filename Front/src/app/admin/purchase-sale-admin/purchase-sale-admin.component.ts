import { Component, OnInit, Input, ViewChild, ElementRef } from '@angular/core';
import { InvoicePrintComponent } from '../../components/invoice-print/invoice-print.component';
import { PrintService } from '../../services/print.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { KioskService } from '../../services/kiosk.service';
import { JalaliDatePipe } from '../../pipes/jalali-date.pipe';
import { Product, Category } from '../../models/kiosk.models';
import {
  RawMaterialService,
  RawMaterial,
  RawMaterialDto,
  RawMaterialTransaction,
  RawMaterialTransactionType
} from '../../services/raw-material.service';
import { RecipeService, RecipeItem } from '../../services/recipe.service';
import { UnitService, UnitOfMeasure, UnitOfMeasureDto } from '../../services/unit.service';
import { PartyService, Party, PartyDto, PartyType, PartyTransaction, PartyTransactionType } from '../../services/party.service';
import { PurchaseInvoiceService, PurchaseInvoiceListItem, PurchaseInvoiceDetail, PurchaseInvoiceItemRequest } from '../../services/purchase-invoice.service';
import { SaleInvoiceService, SaleInvoiceListItem, SaleInvoiceDetail, SaleInvoiceItemRequest } from '../../services/sale-invoice.service';
import { CashAccountService, CashAccount } from '../../services/cash-account.service';

type Section = 'parties' | 'units' | 'materials' | 'purchaseInvoices' | 'products' | 'saleInvoices';

@Component({
  selector: 'app-purchase-sale-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, InvoicePrintComponent, JalaliDatePipe],
  templateUrl: './purchase-sale-admin.component.html',
  styleUrls: ['./purchase-sale-admin.component.css']
})
export class PurchaseSaleAdminComponent implements OnInit {

  activeSection: Section = 'parties';

  @ViewChild(InvoicePrintComponent) invoicePrint!: InvoicePrintComponent;

  private validSections: Section[] = ['parties', 'units', 'materials', 'purchaseInvoices', 'products', 'saleInvoices'];

  @Input() set section(value: string | null) {
    if (value && this.validSections.includes(value as Section)) {
      this.setSection(value as Section);
    }
  }

  TransactionType = RawMaterialTransactionType;
  PartyType = PartyType;
  PartyTransactionType = PartyTransactionType;

  // ---------- طرف‌های حساب ----------
  parties: Party[] = [];
  partyForm: PartyDto = this.emptyPartyForm();
  editingPartyId: number | null = null;
  partyError = '';

  selectedPartyId: number | null = null;
  partyLedger: PartyTransaction[] = [];
  partyLedgerBalance = 0;
  cashForm = { amount: 0, description: '', cashAccountId: 0 };
  cashError = '';
  cashAccounts: CashAccount[] = [];

  // ---------- واحدها ----------
  units: UnitOfMeasure[] = [];
  baseUnits: UnitOfMeasure[] = [];
  unitForm: UnitOfMeasureDto = this.emptyUnitForm();
  editingUnitId: number | null = null;
  unitError = '';

  // ---------- مواد اولیه ----------
  materials: RawMaterial[] = [];
  materialForm: RawMaterialDto = this.emptyMaterialForm();
  editingMaterialId: number | null = null;
  materialError = '';

  consumeForm = this.emptyConsumeForm();
  consumeError = '';

  materialTransactions: RawMaterialTransaction[] = [];

  // ---------- فاکتور خرید ----------
  purchaseInvoices: PurchaseInvoiceListItem[] = [];
  purchaseInvoiceDetail: PurchaseInvoiceDetail | null = null;
  purchaseForm = { partyId: 0, note: '', vatRate: 0 };
  purchaseItems: PurchaseInvoiceItemRequest[] = [];
  newPurchaseItem = { rawMaterialId: 0, unitId: 0, quantity: 0, unitPrice: 0 };
  purchaseError = '';

  // ---------- جستجو ----------
  searchParty = '';
  searchUnit = '';
  searchMaterial = '';
  searchPurchaseInvoice = '';
  searchProduct = '';
  searchSaleInvoice = '';

  get filteredParties(): Party[] {
    const q = this.searchParty.trim().toLowerCase();
    return q ? this.parties.filter(p => p.name.toLowerCase().includes(q)) : this.parties;
  }

  get filteredUnits(): UnitOfMeasure[] {
    const q = this.searchUnit.trim().toLowerCase();
    return q ? this.units.filter(u => u.name.toLowerCase().includes(q)) : this.units;
  }

  get filteredMaterials(): RawMaterial[] {
    const q = this.searchMaterial.trim().toLowerCase();
    return q ? this.materials.filter(m => m.name.toLowerCase().includes(q)) : this.materials;
  }

  get filteredPurchaseInvoices(): PurchaseInvoiceListItem[] {
    const q = this.searchPurchaseInvoice.trim().toLowerCase();
    return q ? this.purchaseInvoices.filter(i =>
      (i.partyName || '').toLowerCase().includes(q) ||
      (i.createdAt || '').toLowerCase().includes(q)
    ) : this.purchaseInvoices;
  }

  get filteredProducts(): Product[] {
    const q = this.searchProduct.trim().toLowerCase();
    return q ? this.products.filter(p => p.name.toLowerCase().includes(q)) : this.products;
  }

  get filteredSaleInvoices(): SaleInvoiceListItem[] {
    const q = this.searchSaleInvoice.trim().toLowerCase();
    return q ? this.saleInvoices.filter(i =>
      (i.partyName || '').toLowerCase().includes(q) ||
      (i.createdAt || '').toLowerCase().includes(q)
    ) : this.saleInvoices;
  }

  // ---------- افزودن سریع ماده اولیه ----------
  showQuickAddMaterial = false;
  quickAddForm: RawMaterialDto = { name: '', unitId: 0, minStockLevel: 0 };
  quickAddError = '';
  quickAddSaving = false;

  // ---------- افزودن سریع طرف حساب ----------
  showQuickAddParty = false;
  quickAddPartyTarget: 'purchase' | 'sale' = 'purchase';
  quickAddPartyForm: PartyDto = this.emptyPartyForm();
  quickAddPartyError = '';
  quickAddPartySaving = false;

  // ---------- افزودن سریع محصول ----------
  categories: Category[] = [];
  showQuickAddProduct = false;
  quickAddProductForm = { name: '', price: 0, categoryId: 0, description: '' };
  quickAddProductError = '';
  quickAddProductSaving = false;

  // ---------- محصولات (فرمول) ----------
  products: Product[] = [];
  selectedProductId: number | null = null;
  recipeItems: RecipeItem[] = [];
  newRecipeItem = { rawMaterialId: 0, quantity: 0 };
  recipeError = '';

  // ---------- فاکتور فروش ----------
  saleInvoices: SaleInvoiceListItem[] = [];
  saleInvoiceDetail: SaleInvoiceDetail | null = null;
  saleForm = { partyId: 0, note: '', vatRate: 0 };
  saleItems: SaleInvoiceItemRequest[] = [];
  newSaleItem = { productId: 0, quantity: 0, unitPrice: 0 };
  saleError = '';

  constructor(
    private rawMaterialService: RawMaterialService,
    private recipeService: RecipeService,
    private unitService: UnitService,
    private partyService: PartyService,
    private purchaseInvoiceService: PurchaseInvoiceService,
    private saleInvoiceService: SaleInvoiceService,
    private cashAccountService: CashAccountService,
    private kioskService: KioskService,
    private printService: PrintService
  ) {}

  @ViewChild('printArea') printAreaRef?: ElementRef<HTMLElement>;
  @ViewChild('printArea2') printAreaRef2?: ElementRef<HTMLElement>;

  printSection(title: string): void {
    if (this.printAreaRef) this.printService.print(title, this.printAreaRef.nativeElement);
  }

  printSection2(title: string): void {
    if (this.printAreaRef2) this.printService.print(title, this.printAreaRef2.nativeElement);
  }

  ngOnInit(): void {
    this.loadParties();
    this.loadUnits();
    this.loadMaterials();
    this.loadMaterialTransactions();
    this.loadProducts();
    this.loadCategories();
    this.loadPurchaseInvoices();
    this.loadSaleInvoices();
    this.loadCashAccounts();
  }

  loadCategories(): void {
    this.kioskService.getCategories().subscribe({
      next: (res) => this.categories = res,
      error: (err) => console.error('خطا در دریافت دسته‌بندی‌ها', err)
    });
  }

  loadCashAccounts(): void {
    this.cashAccountService.getAll(true).subscribe({
      next: (res) => {
        this.cashAccounts = res;
        if (res.length > 0) this.cashForm.cashAccountId = res[0].id;
      },
      error: (err) => console.error('خطا در دریافت صندوق و بانک', err)
    });
  }

  setSection(section: Section): void {
    this.activeSection = section;
  }

  // ===================== طرف‌های حساب =====================
  emptyPartyForm(): PartyDto {
    return { name: '', type: PartyType.Both, phone: '', address: '' };
  }

  loadParties(): void {
    this.partyService.getAll().subscribe({
      next: (res) => this.parties = res,
      error: (err) => console.error('خطا در دریافت طرف‌های حساب', err)
    });
  }

  saveParty(): void {
    this.partyError = '';

    if (!this.partyForm.name) {
      this.partyError = 'نام طرف حساب الزامی است';
      return;
    }

    if (this.editingPartyId) {
      this.partyService.update(this.editingPartyId, this.partyForm).subscribe({
        next: () => { this.cancelEditParty(); this.loadParties(); },
        error: (err) => this.partyError = err?.error?.message || 'خطا در ویرایش طرف حساب'
      });
    } else {
      this.partyService.create(this.partyForm).subscribe({
        next: () => { this.cancelEditParty(); this.loadParties(); },
        error: (err) => this.partyError = err?.error?.message || 'خطا در افزودن طرف حساب'
      });
    }
  }

  editParty(party: Party): void {
    this.editingPartyId = party.id;
    this.partyForm = { name: party.name, type: party.type, phone: party.phone, address: party.address };
    this.partyError = '';
  }

  cancelEditParty(): void {
    this.editingPartyId = null;
    this.partyForm = this.emptyPartyForm();
    this.partyError = '';
  }

  deleteParty(id: number): void {
    if (!confirm('این طرف حساب حذف شود؟')) return;

    this.partyService.delete(id).subscribe({
      next: () => {
        if (this.selectedPartyId === id) this.selectedPartyId = null;
        this.loadParties();
      },
      error: (err) => alert(err?.error?.message || 'خطا در حذف طرف حساب')
    });
  }

  partyTypeLabel(type: PartyType): string {
    switch (type) {
      case PartyType.Supplier: return 'تامین‌کننده';
      case PartyType.Customer: return 'مشتری';
      case PartyType.Both: return 'تامین‌کننده و مشتری';
      default: return '';
    }
  }

  selectParty(id: number): void {
    this.selectedPartyId = id;
    this.cashError = '';
    this.cashForm = { amount: 0, description: '', cashAccountId: this.cashAccounts[0]?.id || 0 };
    this.loadLedger();
  }

  loadLedger(): void {
    if (!this.selectedPartyId) return;

    this.partyService.getLedger(this.selectedPartyId).subscribe({
      next: (res) => {
        this.partyLedger = res.items;
        this.partyLedgerBalance = res.balance;
      },
      error: (err) => console.error('خطا در دریافت دفتر حساب', err)
    });
  }

  get selectedPartyName(): string {
    return this.parties.find(p => p.id === this.selectedPartyId)?.name || '';
  }

  partyTransactionTypeLabel(type: PartyTransactionType): string {
    switch (type) {
      case PartyTransactionType.PurchaseInvoice: return 'فاکتور خرید';
      case PartyTransactionType.SaleInvoice: return 'فاکتور فروش';
      case PartyTransactionType.Payment: return 'پرداخت';
      case PartyTransactionType.Receipt: return 'دریافت';
      default: return '';
    }
  }

  submitPayment(): void {
    this.cashError = '';

    if (!this.selectedPartyId || this.cashForm.amount <= 0) {
      this.cashError = 'مقدار باید بیشتر از صفر باشد';
      return;
    }

    if (!this.cashForm.cashAccountId) {
      this.cashError = 'انتخاب صندوق/بانک الزامی است';
      return;
    }

    this.partyService.payment(this.selectedPartyId, this.cashForm).subscribe({
      next: () => { this.resetCashForm(); this.loadLedger(); this.loadParties(); this.loadCashAccounts(); },
      error: (err) => this.cashError = err?.error?.message || 'خطا در ثبت پرداخت'
    });
  }

  submitReceipt(): void {
    this.cashError = '';

    if (!this.selectedPartyId || this.cashForm.amount <= 0) {
      this.cashError = 'مقدار باید بیشتر از صفر باشد';
      return;
    }

    if (!this.cashForm.cashAccountId) {
      this.cashError = 'انتخاب صندوق/بانک الزامی است';
      return;
    }

    this.partyService.receipt(this.selectedPartyId, this.cashForm).subscribe({
      next: () => { this.resetCashForm(); this.loadLedger(); this.loadParties(); this.loadCashAccounts(); },
      error: (err) => this.cashError = err?.error?.message || 'خطا در ثبت دریافت'
    });
  }

  private resetCashForm(): void {
    this.cashForm = { amount: 0, description: '', cashAccountId: this.cashAccounts[0]?.id || 0 };
  }

  // ===================== واحدها =====================
  emptyUnitForm(): UnitOfMeasureDto {
    return { name: '', baseUnitId: null, conversionFactor: 1 };
  }

  loadUnits(): void {
    this.unitService.getAll().subscribe({
      next: (res) => {
        this.units = res;
        this.baseUnits = res.filter(u => u.baseUnitId === null);
      },
      error: (err) => console.error('خطا در دریافت واحدها', err)
    });
  }

  saveUnit(): void {
    this.unitError = '';

    if (!this.unitForm.name) {
      this.unitError = 'نام واحد الزامی است';
      return;
    }

    if (this.unitForm.baseUnitId && this.unitForm.conversionFactor <= 0) {
      this.unitError = 'ضریب تبدیل باید بیشتر از صفر باشد';
      return;
    }

    if (this.editingUnitId) {
      this.unitService.update(this.editingUnitId, this.unitForm).subscribe({
        next: () => { this.cancelEditUnit(); this.loadUnits(); },
        error: (err) => this.unitError = err?.error?.message || 'خطا در ویرایش واحد'
      });
    } else {
      this.unitService.create(this.unitForm).subscribe({
        next: () => { this.cancelEditUnit(); this.loadUnits(); },
        error: (err) => this.unitError = err?.error?.message || 'خطا در افزودن واحد'
      });
    }
  }

  editUnit(unit: UnitOfMeasure): void {
    this.editingUnitId = unit.id;
    this.unitForm = { name: unit.name, baseUnitId: unit.baseUnitId, conversionFactor: unit.conversionFactor };
    this.unitError = '';
  }

  cancelEditUnit(): void {
    this.editingUnitId = null;
    this.unitForm = this.emptyUnitForm();
    this.unitError = '';
  }

  deleteUnit(id: number): void {
    if (!confirm('این واحد حذف شود؟')) return;

    this.unitService.delete(id).subscribe({
      next: () => this.loadUnits(),
      error: (err) => alert(err?.error?.message || 'خطا در حذف واحد')
    });
  }

  // ===================== مواد اولیه =====================
  emptyMaterialForm(): RawMaterialDto {
    return { name: '', unitId: 0, minStockLevel: 0 };
  }

  emptyConsumeForm() {
    return { rawMaterialId: 0, quantity: 0, note: '' };
  }

  loadMaterials(): void {
    this.rawMaterialService.getAll().subscribe({
      next: (res) => this.materials = res,
      error: (err) => console.error('خطا در دریافت مواد اولیه', err)
    });
  }

  saveMaterial(): void {
    this.materialError = '';

    if (!this.materialForm.name) {
      this.materialError = 'نام ماده اولیه الزامی است';
      return;
    }

    if (!this.materialForm.unitId) {
      this.materialError = 'انتخاب واحد الزامی است';
      return;
    }

    if (this.editingMaterialId) {
      this.rawMaterialService.update(this.editingMaterialId, this.materialForm).subscribe({
        next: () => { this.cancelEditMaterial(); this.loadMaterials(); },
        error: (err) => this.materialError = err?.error?.message || 'خطا در ویرایش ماده اولیه'
      });
    } else {
      this.rawMaterialService.create(this.materialForm).subscribe({
        next: () => { this.cancelEditMaterial(); this.loadMaterials(); },
        error: (err) => this.materialError = err?.error?.message || 'خطا در افزودن ماده اولیه'
      });
    }
  }

  editMaterial(material: RawMaterial): void {
    this.editingMaterialId = material.id;
    this.materialForm = { name: material.name, unitId: material.unitId, minStockLevel: material.minStockLevel };
    this.materialError = '';
  }

  cancelEditMaterial(): void {
    this.editingMaterialId = null;
    this.materialForm = this.emptyMaterialForm();
    this.materialError = '';
  }

  deleteMaterial(id: number): void {
    if (!confirm('این ماده اولیه حذف شود؟')) return;

    this.rawMaterialService.delete(id).subscribe({
      next: () => this.loadMaterials(),
      error: (err) => alert(err?.error?.message || 'خطا در حذف ماده اولیه')
    });
  }

  // مواد اولیه: پایه‌ها (برای انتخاب واحد ذخیره) - فقط واحدهای پایه
  get baseUnitOptions(): UnitOfMeasure[] {
    return this.baseUnits;
  }

  submitConsume(): void {
    this.consumeError = '';

    if (!this.consumeForm.rawMaterialId || this.consumeForm.quantity <= 0) {
      this.consumeError = 'انتخاب ماده اولیه و مقدار معتبر الزامی است';
      return;
    }

    this.rawMaterialService.consume(this.consumeForm).subscribe({
      next: () => {
        this.consumeForm = this.emptyConsumeForm();
        this.loadMaterials();
        this.loadMaterialTransactions();
      },
      error: (err) => this.consumeError = err?.error?.message || 'خطا در ثبت خروج'
    });
  }

  loadMaterialTransactions(): void {
    this.rawMaterialService.getTransactions().subscribe({
      next: (res) => this.materialTransactions = res.items,
      error: (err) => console.error('خطا در دریافت تراکنش‌های مواد اولیه', err)
    });
  }

  materialTransactionTypeLabel(type: RawMaterialTransactionType): string {
    switch (type) {
      case RawMaterialTransactionType.In: return 'خرید / ورود';
      case RawMaterialTransactionType.Out: return 'مصرف / خروج';
      case RawMaterialTransactionType.Adjustment: return 'اصلاح موجودی';
      default: return '';
    }
  }

  // واحدهای قابل استفاده برای یک ماده اولیه (واحد خودش + واحدهای زیرمجموعه آن)
  unitsForMaterial(materialId: number): UnitOfMeasure[] {
    const material = this.materials.find(m => m.id === materialId);
    if (!material) return [];

    return this.units.filter(u => u.id === material.unitId || u.baseUnitId === material.unitId);
  }

  // ===================== فاکتور خرید =====================
  loadPurchaseInvoices(): void {
    this.purchaseInvoiceService.getAll().subscribe({
      next: (res) => this.purchaseInvoices = res.items,
      error: (err) => console.error('خطا در دریافت فاکتورهای خرید', err)
    });
  }

  onPurchaseMaterialChange(): void {
    const opts = this.unitsForMaterial(this.newPurchaseItem.rawMaterialId);
    this.newPurchaseItem.unitId = opts.length > 0 ? opts[0].id : 0;
  }

  addPurchaseItem(): void {
    this.purchaseError = '';

    if (!this.newPurchaseItem.rawMaterialId || !this.newPurchaseItem.unitId || this.newPurchaseItem.quantity <= 0 || this.newPurchaseItem.unitPrice < 0) {
      this.purchaseError = 'انتخاب ماده اولیه، واحد و مقادیر معتبر الزامی است';
      return;
    }

    this.purchaseItems.push({
      rawMaterialId: this.newPurchaseItem.rawMaterialId,
      unitId: this.newPurchaseItem.unitId,
      quantity: this.newPurchaseItem.quantity,
      unitPrice: this.newPurchaseItem.unitPrice
    });

    this.newPurchaseItem = { rawMaterialId: 0, unitId: 0, quantity: 0, unitPrice: 0 };
  }

  removePurchaseItem(index: number): void {
    this.purchaseItems.splice(index, 1);
  }

  openQuickAddMaterial(): void {
    this.quickAddForm = { name: '', unitId: 0, minStockLevel: 0 };
    this.quickAddError = '';
    this.quickAddSaving = false;
    this.showQuickAddMaterial = true;
    setTimeout(() => {
      const el = document.getElementById('quickAddMaterialName');
      if (el) el.focus();
    }, 100);
  }

  closeQuickAddMaterial(): void {
    this.showQuickAddMaterial = false;
    this.quickAddError = '';
  }

  saveQuickAddMaterial(): void {
    this.quickAddError = '';
    if (!this.quickAddForm.name.trim()) {
      this.quickAddError = 'نام ماده اولیه الزامی است';
      return;
    }
    if (!this.quickAddForm.unitId) {
      this.quickAddError = 'انتخاب واحد الزامی است';
      return;
    }
    this.quickAddSaving = true;
    this.rawMaterialService.create(this.quickAddForm).subscribe({
      next: (created) => {
        this.rawMaterialService.getAll().subscribe({
          next: (list) => {
            this.materials = list;
            this.newPurchaseItem.rawMaterialId = created.id;
            this.onPurchaseMaterialChange();
            this.showQuickAddMaterial = false;
            this.quickAddSaving = false;
          },
          error: () => {
            this.showQuickAddMaterial = false;
            this.quickAddSaving = false;
          }
        });
      },
      error: (err) => {
        this.quickAddError = err?.error?.message || 'خطا در ثبت ماده اولیه';
        this.quickAddSaving = false;
      }
    });
  }

  openQuickAddParty(target: 'purchase' | 'sale' = 'purchase'): void {
    this.quickAddPartyTarget = target;
    this.quickAddPartyForm = this.emptyPartyForm();
    this.quickAddPartyError = '';
    this.quickAddPartySaving = false;
    this.showQuickAddParty = true;
    setTimeout(() => {
      const el = document.getElementById('quickAddPartyName');
      if (el) el.focus();
    }, 100);
  }

  closeQuickAddParty(): void {
    this.showQuickAddParty = false;
    this.quickAddPartyError = '';
  }

  saveQuickAddParty(): void {
    this.quickAddPartyError = '';
    if (!this.quickAddPartyForm.name.trim()) {
      this.quickAddPartyError = 'نام طرف حساب الزامی است';
      return;
    }
    this.quickAddPartySaving = true;
    this.partyService.create(this.quickAddPartyForm).subscribe({
      next: (created) => {
        this.partyService.getAll().subscribe({
          next: (list) => {
            this.parties = list;
            if (this.quickAddPartyTarget === 'sale') {
              this.saleForm.partyId = created.id;
            } else {
              this.purchaseForm.partyId = created.id;
            }
            this.showQuickAddParty = false;
            this.quickAddPartySaving = false;
          },
          error: () => {
            this.showQuickAddParty = false;
            this.quickAddPartySaving = false;
          }
        });
      },
      error: (err) => {
        this.quickAddPartyError = err?.error?.message || 'خطا در ثبت طرف حساب';
        this.quickAddPartySaving = false;
      }
    });
  }

  openQuickAddProduct(): void {
    this.quickAddProductForm = { name: '', price: 0, categoryId: 0, description: '' };
    this.quickAddProductError = '';
    this.quickAddProductSaving = false;
    this.showQuickAddProduct = true;
    setTimeout(() => {
      const el = document.getElementById('quickAddProductName');
      if (el) el.focus();
    }, 100);
  }

  closeQuickAddProduct(): void {
    this.showQuickAddProduct = false;
    this.quickAddProductError = '';
  }

  saveQuickAddProduct(): void {
    this.quickAddProductError = '';
    if (!this.quickAddProductForm.name.trim()) {
      this.quickAddProductError = 'نام محصول الزامی است';
      return;
    }
    if (!this.quickAddProductForm.price || this.quickAddProductForm.price <= 0) {
      this.quickAddProductError = 'قیمت محصول الزامی است';
      return;
    }
    if (!this.quickAddProductForm.categoryId) {
      this.quickAddProductError = 'انتخاب دسته‌بندی الزامی است';
      return;
    }
    this.quickAddProductSaving = true;
    this.kioskService.addProduct(this.quickAddProductForm).subscribe({
      next: (created: any) => {
        this.kioskService.getAllProducts().subscribe({
          next: (list) => {
            this.products = list;
            this.newSaleItem.productId = created.id;
            this.showQuickAddProduct = false;
            this.quickAddProductSaving = false;
          },
          error: () => {
            this.showQuickAddProduct = false;
            this.quickAddProductSaving = false;
          }
        });
      },
      error: (err: any) => {
        this.quickAddProductError = err?.error?.message || 'خطا در ثبت محصول';
        this.quickAddProductSaving = false;
      }
    });
  }

  purchaseItemMaterialName(rawMaterialId: number): string {
    return this.materials.find(m => m.id === rawMaterialId)?.name || '';
  }

  purchaseItemUnitName(unitId: number): string {
    return this.units.find(u => u.id === unitId)?.name || '';
  }

  get purchaseTotal(): number {
    return this.purchaseItems.reduce((sum, i) => sum + (i.quantity * i.unitPrice), 0);
  }

  get purchaseVatAmount(): number {
    return Math.round(this.purchaseTotal * (this.purchaseForm.vatRate || 0) / 100 * 100) / 100;
  }

  get purchaseGrandTotal(): number {
    return this.purchaseTotal + this.purchaseVatAmount;
  }

  submitPurchaseInvoice(): void {
    this.purchaseError = '';

    if (!this.purchaseForm.partyId) {
      this.purchaseError = 'انتخاب طرف حساب الزامی است';
      return;
    }

    if (this.purchaseItems.length === 0) {
      this.purchaseError = 'حداقل یک قلم کالا الزامی است';
      return;
    }

    this.purchaseInvoiceService.create({
      partyId: this.purchaseForm.partyId,
      note: this.purchaseForm.note,
      vatRate: this.purchaseForm.vatRate,
      items: this.purchaseItems
    }).subscribe({
      next: () => {
        this.purchaseForm = { partyId: 0, note: '', vatRate: 0 };
        this.purchaseItems = [];
        this.loadPurchaseInvoices();
        this.loadMaterials();
        this.loadMaterialTransactions();
        this.loadParties();
        if (this.selectedPartyId) this.loadLedger();
      },
      error: (err) => this.purchaseError = err?.error?.message || 'خطا در ثبت فاکتور خرید'
    });
  }

  viewPurchaseInvoice(id: number): void {
    this.purchaseInvoiceService.getOne(id).subscribe({
      next: (res) => this.purchaseInvoiceDetail = res,
      error: (err) => console.error('خطا در دریافت فاکتور خرید', err)
    });
  }

  closePurchaseInvoiceDetail(): void {
    this.purchaseInvoiceDetail = null;
  }

  printPurchaseInvoice(id: number): void {
    this.purchaseInvoiceService.getOne(id).subscribe({
      next: (res) => {
        this.invoicePrint.open({
          invoiceTypeLabel: 'فاکتور خرید',
          invoiceNumber: res.id,
          partyLabel: 'تامین‌کننده',
          partyName: res.partyName,
          date: res.createdAt,
          items: res.items.map(it => ({
            name: it.rawMaterialName,
            unit: it.unitName,
            quantity: it.quantity,
            unitPrice: it.unitPrice,
            totalPrice: it.totalPrice
          })),
          subtotal: res.totalAmount,
          vatRate: res.vatRate,
          vatAmount: res.vatAmount,
          grandTotal: res.grandTotal,
          note: res.note
        });
      },
      error: (err) => console.error('خطا در دریافت فاکتور خرید برای چاپ', err)
    });
  }

  // ===================== محصولات (فرمول) =====================
  loadProducts(): void {
    this.kioskService.getAllProducts().subscribe({
      next: (res) => this.products = res,
      error: (err) => console.error('خطا در دریافت محصولات', err)
    });
  }

  onProductSelect(): void {
    this.recipeError = '';
    this.recipeItems = [];

    if (!this.selectedProductId) return;

    this.recipeService.getByProduct(this.selectedProductId).subscribe({
      next: (res) => this.recipeItems = res.items,
      error: (err) => this.recipeError = err?.error?.message || 'خطا در دریافت فرمول محصول'
    });
  }

  addRecipeItem(): void {
    if (!this.newRecipeItem.rawMaterialId || this.newRecipeItem.quantity <= 0) return;

    if (this.recipeItems.some(i => i.rawMaterialId === this.newRecipeItem.rawMaterialId)) {
      this.recipeError = 'این ماده اولیه قبلاً به فرمول اضافه شده است';
      return;
    }

    const material = this.materials.find(m => m.id === this.newRecipeItem.rawMaterialId);

    this.recipeItems.push({
      rawMaterialId: this.newRecipeItem.rawMaterialId,
      rawMaterialName: material?.name,
      unit: material?.unitName,
      quantity: this.newRecipeItem.quantity
    });

    this.newRecipeItem = { rawMaterialId: 0, quantity: 0 };
    this.recipeError = '';
  }

  removeRecipeItem(index: number): void {
    this.recipeItems.splice(index, 1);
  }

  saveRecipe(): void {
    if (!this.selectedProductId) return;

    this.recipeError = '';

    this.recipeService.setRecipe(this.selectedProductId, this.recipeItems).subscribe({
      next: () => this.onProductSelect(),
      error: (err) => this.recipeError = err?.error?.message || 'خطا در ذخیره فرمول'
    });
  }

  // ===================== فاکتور فروش =====================
  loadSaleInvoices(): void {
    this.saleInvoiceService.getAll().subscribe({
      next: (res) => this.saleInvoices = res.items,
      error: (err) => console.error('خطا در دریافت فاکتورهای فروش', err)
    });
  }

  addSaleItem(): void {
    this.saleError = '';

    if (!this.newSaleItem.productId || this.newSaleItem.quantity <= 0 || this.newSaleItem.unitPrice < 0) {
      this.saleError = 'انتخاب محصول و مقادیر معتبر الزامی است';
      return;
    }

    this.saleItems.push({
      productId: this.newSaleItem.productId,
      quantity: this.newSaleItem.quantity,
      unitPrice: this.newSaleItem.unitPrice
    });

    this.newSaleItem = { productId: 0, quantity: 0, unitPrice: 0 };
  }

  removeSaleItem(index: number): void {
    this.saleItems.splice(index, 1);
  }

  saleItemProductName(productId: number): string {
    return this.products.find(p => p.id === productId)?.name || '';
  }

  get saleTotal(): number {
    return this.saleItems.reduce((sum, i) => sum + (i.quantity * i.unitPrice), 0);
  }

  get saleVatAmount(): number {
    return Math.round(this.saleTotal * (this.saleForm.vatRate || 0) / 100 * 100) / 100;
  }

  get saleGrandTotal(): number {
    return this.saleTotal + this.saleVatAmount;
  }

  submitSaleInvoice(): void {
    this.saleError = '';

    if (!this.saleForm.partyId) {
      this.saleError = 'انتخاب طرف حساب الزامی است';
      return;
    }

    if (this.saleItems.length === 0) {
      this.saleError = 'حداقل یک قلم کالا الزامی است';
      return;
    }

    this.saleInvoiceService.create({
      partyId: this.saleForm.partyId,
      note: this.saleForm.note,
      vatRate: this.saleForm.vatRate,
      items: this.saleItems
    }).subscribe({
      next: () => {
        this.saleForm = { partyId: 0, note: '', vatRate: 0 };
        this.saleItems = [];
        this.loadSaleInvoices();
        this.loadMaterials();
        this.loadMaterialTransactions();
        this.loadParties();
        if (this.selectedPartyId) this.loadLedger();
      },
      error: (err) => this.saleError = err?.error?.message || 'خطا در ثبت فاکتور فروش'
    });
  }

  viewSaleInvoice(id: number): void {
    this.saleInvoiceService.getOne(id).subscribe({
      next: (res) => this.saleInvoiceDetail = res,
      error: (err) => console.error('خطا در دریافت فاکتور فروش', err)
    });
  }

  closeSaleInvoiceDetail(): void {
    this.saleInvoiceDetail = null;
  }

  printSaleInvoice(id: number): void {
    this.saleInvoiceService.getOne(id).subscribe({
      next: (res) => {
        this.invoicePrint.open({
          invoiceTypeLabel: 'فاکتور فروش',
          invoiceNumber: res.id,
          partyLabel: 'مشتری',
          partyName: res.partyName,
          date: res.createdAt,
          items: res.items.map(it => ({
            name: it.productName,
            quantity: it.quantity,
            unitPrice: it.unitPrice,
            totalPrice: it.totalPrice
          })),
          subtotal: res.totalAmount,
          vatRate: res.vatRate,
          vatAmount: res.vatAmount,
          grandTotal: res.grandTotal,
          note: res.note
        });
      },
      error: (err) => console.error('خطا در دریافت فاکتور فروش برای چاپ', err)
    });
  }
}
