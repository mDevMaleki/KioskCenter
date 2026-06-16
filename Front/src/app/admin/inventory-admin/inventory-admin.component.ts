import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { KioskService } from '../../services/kiosk.service';
import { StyleService } from '../../services/style.service';
import { InventoryService, InventoryProduct, InventoryTransaction } from '../../services/inventory.service';

type StockModalMode = 'in' | 'out' | 'adjust' | 'settings';

@Component({
  selector: 'app-inventory-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './inventory-admin.component.html',
  styleUrls: ['./inventory-admin.component.css']
})
export class InventoryAdminComponent implements OnInit {

  products: InventoryProduct[] = [];
  categories: any[] = [];

  loading = false;
  searchTerm = '';
  selectedCategoryId: number | null = null;
  lowStockOnly = false;

  // مودال ورود/خروج/اصلاح
  showStockModal = false;
  stockModalMode: StockModalMode = 'in';
  selectedProduct: InventoryProduct | null = null;
  stockForm = {
    quantity: 0,
    unitPrice: null as number | null,
    newQuantity: 0,
    minStockLevel: 0,
    unit: 'عدد',
    note: ''
  };
  savingStock = false;
  stockError = '';

  // مودال تاریخچه
  showHistoryModal = false;
  historyProduct: InventoryProduct | null = null;
  transactions: InventoryTransaction[] = [];
  loadingHistory = false;

  constructor(
    private kioskService: KioskService,
    private styleService: StyleService,
    private inventoryService: InventoryService
  ) {}

  ngOnInit(): void {
    this.loadCategories();
    this.loadProducts();
  }

  loadCategories(): void {
    this.kioskService.getCategories().subscribe({
      next: (res: any) => this.categories = res,
      error: err => console.error('خطا در دریافت دسته‌بندی‌ها', err)
    });
  }

  loadProducts(): void {
    this.loading = true;
    this.inventoryService.getProductsStock({
      categoryId: this.selectedCategoryId || undefined,
      search: this.searchTerm || undefined,
      lowStockOnly: this.lowStockOnly
    }).subscribe({
      next: (res) => {
        this.products = res;
        this.loading = false;
      },
      error: err => {
        console.error('خطا در دریافت موجودی', err);
        this.loading = false;
      }
    });
  }

  getAssetUrl(url: string | null | undefined): string {
    return this.styleService.resolveAssetUrl(url || '');
  }

  isLowStock(p: InventoryProduct): boolean {
    return p.stockQuantity <= p.minStockLevel;
  }

  // ===== مودال ورود/خروج/اصلاح/تنظیمات =====
  openStockModal(product: InventoryProduct, mode: StockModalMode): void {
    this.selectedProduct = product;
    this.stockModalMode = mode;
    this.stockError = '';
    this.stockForm = {
      quantity: 0,
      unitPrice: null,
      newQuantity: product.stockQuantity,
      minStockLevel: product.minStockLevel,
      unit: product.unit || 'عدد',
      note: ''
    };
    this.showStockModal = true;
  }

  closeStockModal(): void {
    this.showStockModal = false;
    this.selectedProduct = null;
  }

  get stockModalTitle(): string {
    switch (this.stockModalMode) {
      case 'in': return 'ورود کالا';
      case 'out': return 'خروج کالا';
      case 'adjust': return 'اصلاح موجودی';
      case 'settings': return 'تنظیمات انبار';
      default: return '';
    }
  }

  submitStockModal(): void {
    if (!this.selectedProduct) return;
    this.stockError = '';

    if (this.stockModalMode === 'in' || this.stockModalMode === 'out') {
      if (!this.stockForm.quantity || this.stockForm.quantity <= 0) {
        this.stockError = 'مقدار باید بیشتر از صفر باشد';
        return;
      }
    }

    this.savingStock = true;
    const productId = this.selectedProduct.id;

    let request$;
    if (this.stockModalMode === 'in') {
      request$ = this.inventoryService.stockIn({
        productId,
        quantity: this.stockForm.quantity,
        unitPrice: this.stockForm.unitPrice,
        note: this.stockForm.note
      });
    } else if (this.stockModalMode === 'out') {
      request$ = this.inventoryService.stockOut({
        productId,
        quantity: this.stockForm.quantity,
        note: this.stockForm.note
      });
    } else if (this.stockModalMode === 'adjust') {
      request$ = this.inventoryService.adjustStock({
        productId,
        newQuantity: this.stockForm.newQuantity,
        note: this.stockForm.note
      });
    } else {
      request$ = this.inventoryService.updateSettings(productId, {
        minStockLevel: this.stockForm.minStockLevel,
        unit: this.stockForm.unit
      });
    }

    request$.subscribe({
      next: () => {
        this.savingStock = false;
        this.closeStockModal();
        this.loadProducts();
      },
      error: (err) => {
        this.savingStock = false;
        this.stockError = err?.error?.message || 'خطا در ثبت اطلاعات';
      }
    });
  }

  // ===== مودال تاریخچه =====
  openHistory(product: InventoryProduct | null = null): void {
    this.historyProduct = product;
    this.showHistoryModal = true;
    this.loadTransactions();
  }

  closeHistoryModal(): void {
    this.showHistoryModal = false;
    this.historyProduct = null;
    this.transactions = [];
  }

  loadTransactions(): void {
    this.loadingHistory = true;
    this.inventoryService.getTransactions({
      productId: this.historyProduct?.id,
      pageSize: 100
    }).subscribe({
      next: (res) => {
        this.transactions = res.items;
        this.loadingHistory = false;
      },
      error: err => {
        console.error('خطا در دریافت تاریخچه', err);
        this.loadingHistory = false;
      }
    });
  }

  transactionLabel(type: string): string {
    switch (type) {
      case 'In': return 'ورود';
      case 'Out': return 'خروج';
      case 'Adjustment': return 'اصلاح';
      default: return type;
    }
  }

  formatDate(value: string): string {
    return new Date(value).toLocaleString('fa-IR');
  }
}
