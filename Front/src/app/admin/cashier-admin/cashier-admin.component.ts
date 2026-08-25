import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { KioskService } from '../../services/kiosk.service';
import { Category, Product } from '../../models/kiosk.models';
import { PaymentMethod, PaymentMethodService } from '../../services/payment-method.service';
import { CashierService, ParkedOrder } from '../../services/cashier.service';

interface CartLine {
  productId: number;
  productName: string;
  unitPrice: number;
  quantity: number;
}

@Component({
  selector: 'app-cashier-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cashier-admin.component.html',
  styleUrls: ['./cashier-admin.component.css']
})
export class CashierAdminComponent implements OnInit {
  categories: Category[] = [];
  products: Product[] = [];
  selectedCategoryId: number | null = null;
  searchTerm = '';

  cart: CartLine[] = [];
  customerName = '';

  paymentMethods: PaymentMethod[] = [];
  parkedOrders: ParkedOrder[] = [];

  showParkPrompt = false;
  parkLabel = '';

  showPaymentPicker = false;
  checkoutTargetOrderId: number | null = null; // اگر مقدار داشت یعنی تسویه سفارش پارک‌شده‌ی موجود

  showParkedDrawer = false;

  toggleParkedDrawer(): void {
    this.showParkedDrawer = !this.showParkedDrawer;
  }

  busy = false;
  message = '';
  messageType: 'success' | 'error' = 'success';

  constructor(
    private kioskService: KioskService,
    private paymentMethodService: PaymentMethodService,
    private cashierService: CashierService
  ) {}

  ngOnInit(): void {
    this.loadProducts();
    this.loadCategories();
    this.loadPaymentMethods();
    this.loadParkedOrders();
  }

  loadProducts(): void {
    this.kioskService.getAllProducts().subscribe(p => this.products = p);
  }

  loadCategories(): void {
    this.kioskService.getCategories().subscribe(c => this.categories = c);
  }

  loadPaymentMethods(): void {
    this.paymentMethodService.getAllMethods().subscribe(m => this.paymentMethods = m.filter(x => x.isActive));
  }

  loadParkedOrders(): void {
    this.cashierService.getParkedOrders().subscribe(o => this.parkedOrders = o);
  }

  get filteredProducts(): Product[] {
    return this.products.filter(p => {
      const matchCategory = !this.selectedCategoryId || p.categoryId === this.selectedCategoryId;
      const matchSearch = !this.searchTerm || p.name.toLowerCase().includes(this.searchTerm.toLowerCase());
      return matchCategory && matchSearch;
    });
  }

  get cartTotal(): number {
    return this.cart.reduce((sum, l) => sum + l.unitPrice * l.quantity, 0);
  }

  get cartCount(): number {
    return this.cart.reduce((sum, l) => sum + l.quantity, 0);
  }

  addToCart(product: Product): void {
    const existing = this.cart.find(l => l.productId === product.id);
    if (existing) {
      existing.quantity++;
    } else {
      this.cart.push({ productId: product.id, productName: product.name, unitPrice: product.price, quantity: 1 });
    }
  }

  increaseQty(line: CartLine): void {
    line.quantity++;
  }

  decreaseQty(line: CartLine): void {
    line.quantity--;
    if (line.quantity <= 0) {
      this.cart = this.cart.filter(l => l !== line);
    }
  }

  removeLine(line: CartLine): void {
    this.cart = this.cart.filter(l => l !== line);
  }

  clearCart(): void {
    this.cart = [];
    this.customerName = '';
    this.checkoutTargetOrderId = null;
  }

  private showMessage(text: string, type: 'success' | 'error' = 'success'): void {
    this.message = text;
    this.messageType = type;
    setTimeout(() => { this.message = ''; }, 4000);
  }

  // ---------- پارک کردن سفارش ----------
  openParkPrompt(): void {
    if (this.cart.length === 0) {
      this.showMessage('سبد خالی است', 'error');
      return;
    }
    this.parkLabel = this.customerName || '';
    this.showParkPrompt = true;
  }

  cancelPark(): void {
    this.showParkPrompt = false;
  }

  confirmPark(): void {
    this.busy = true;
    this.cashierService.createOrder({
      customerName: this.customerName,
      orderType: 'EatIn',
      items: this.cart.map(l => ({ productId: l.productId, quantity: l.quantity })),
      park: true,
      parkedLabel: this.parkLabel
    }).subscribe({
      next: () => {
        this.busy = false;
        this.showParkPrompt = false;
        this.showMessage('سفارش پارک شد');
        this.clearCart();
        this.loadParkedOrders();
      },
      error: () => {
        this.busy = false;
        this.showMessage('خطا در پارک کردن سفارش', 'error');
      }
    });
  }

  // ---------- ازسرگیری سفارش پارک‌شده برای ویرایش ----------
  resumeOrder(order: ParkedOrder): void {
    this.cart = order.orderItems.map(it => ({
      productId: it.productId,
      productName: it.product?.name || `محصول #${it.productId}`,
      unitPrice: it.unitPrice,
      quantity: it.quantity
    }));
    this.customerName = order.customerName || order.parkedLabel || '';
    this.busy = true;
    this.cashierService.deleteOrder(order.id).subscribe({
      next: () => {
        this.busy = false;
        this.showParkedDrawer = false;
        this.loadParkedOrders();
      },
      error: () => { this.busy = false; }
    });
  }

  removeParkedOrder(order: ParkedOrder): void {
    if (!confirm('این سفارش پارک‌شده حذف شود؟')) return;
    this.cashierService.deleteOrder(order.id).subscribe(() => this.loadParkedOrders());
  }

  // ---------- تسویه ----------
  openCheckoutForCart(): void {
    if (this.cart.length === 0) {
      this.showMessage('سبد خالی است', 'error');
      return;
    }
    this.checkoutTargetOrderId = null;
    this.showPaymentPicker = true;
  }

  openCheckoutForParked(order: ParkedOrder): void {
    this.checkoutTargetOrderId = order.id;
    this.showPaymentPicker = true;
  }

  cancelPaymentPicker(): void {
    this.showPaymentPicker = false;
    this.checkoutTargetOrderId = null;
  }

  selectPaymentMethod(method: PaymentMethod): void {
    this.busy = true;
    if (this.checkoutTargetOrderId) {
      // تسویه سفارش پارک‌شده موجود
      const orderId = this.checkoutTargetOrderId;
      this.cashierService.confirmPayment(orderId, method.id).subscribe({
        next: () => {
          this.finishCheckout(orderId);
          this.loadParkedOrders();
        },
        error: () => { this.busy = false; this.showMessage('خطا در تسویه سفارش', 'error'); }
      });
    } else {
      // ایجاد و تسویه سفارش جدید از سبد فعلی
      this.cashierService.createOrder({
        customerName: this.customerName,
        orderType: 'EatIn',
        items: this.cart.map(l => ({ productId: l.productId, quantity: l.quantity })),
        paymentMethodId: method.id,
        park: false
      }).subscribe({
        next: (res) => {
          const orderId = res.orderId;
          this.cashierService.confirmPayment(orderId).subscribe({
            next: () => {
              this.finishCheckout(orderId);
              this.clearCart();
            },
            error: () => { this.busy = false; this.showMessage('خطا در تأیید پرداخت', 'error'); }
          });
        },
        error: () => { this.busy = false; this.showMessage('خطا در ثبت سفارش', 'error'); }
      });
    }
  }

  private finishCheckout(orderId: number): void {
    this.cashierService.printReceipt(orderId).subscribe({
      next: () => {
        this.busy = false;
        this.showPaymentPicker = false;
        this.checkoutTargetOrderId = null;
        this.showMessage('تسویه و چاپ فیش انجام شد');
      },
      error: () => {
        this.busy = false;
        this.showPaymentPicker = false;
        this.checkoutTargetOrderId = null;
        this.showMessage('تسویه انجام شد، اما چاپ فیش با خطا مواجه شد', 'error');
      }
    });
  }
}
