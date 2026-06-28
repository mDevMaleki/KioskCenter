import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import {
  Category,
  Product,
  CartItem,
  KioskState,
  CreateOrderDto,
  PosPaymentResponse
} from '../models/kiosk.models';

@Injectable({
  providedIn: 'root'
})
export class KioskService {
  private apiUrl = 'http://localhost:5000/api';

  private state = new BehaviorSubject<KioskState>({
    currentStep: 1,
    selectedCategory: null,
    cart: [],
    orderType: '',
    customerName: ''
  });

  state$ = this.state.asObservable();

  constructor(private http: HttpClient) { }

  getAllProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.apiUrl}/product`);
  }

  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.apiUrl}/category`);
  }

  getProductsByCategory(categoryId: number): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.apiUrl}/product/category/${categoryId}`);
  }

  createOrder(orderData: CreateOrderDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/order/create`, orderData);
  }

  confirmPayment(orderId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/order/confirm/${orderId}`, {});
  }

  setCategory(category: Category): void {
    const current = this.state.value;
    this.state.next({
      ...current,
      selectedCategory: category,
      currentStep: 2
    });
  }

  addToCart(product: Product): void {
    const current = this.state.value;
    const existingItem = current.cart.find(item => item.productId === product.id);

    let newCart;
    if (existingItem) {
      newCart = current.cart.map(item =>
        item.productId === product.id
          ? { ...item, quantity: item.quantity + 1, totalPrice: (item.quantity + 1) * item.price }
          : item
      );
    } else {
      newCart = [...current.cart, {
        productId: product.id,
        productName: product.name,
        price: product.price,
        quantity: 1,
        totalPrice: product.price
      }];
    }

    this.state.next({ ...current, cart: newCart });
  }

  updateQuantity(productId: number, delta: number): void {
    const current = this.state.value;
    const newCart = current.cart
      .map(item => {
        if (item.productId === productId) {
          const newQuantity = item.quantity + delta;
          if (newQuantity <= 0) return null;
          return { ...item, quantity: newQuantity, totalPrice: newQuantity * item.price };
        }
        return item;
      })
      .filter(item => item !== null) as CartItem[];

    this.state.next({ ...current, cart: newCart });
  }

  removeFromCart(productId: number): void {
    const current = this.state.value;
    const newCart = current.cart.filter(item => item.productId !== productId);
    this.state.next({ ...current, cart: newCart });
  }

  setOrderType(orderType: string): void {
    const current = this.state.value;
    this.state.next({
      ...current,
      orderType: orderType,
      currentStep: 3
    });
  }

  setCustomerName(name: string): void {
    const current = this.state.value;
    this.state.next({ ...current, customerName: name });
  }

  goBack(): void {
    const current = this.state.value;
    if (current.currentStep > 1) {
      this.state.next({ ...current, currentStep: current.currentStep - 1 });
    }
  }

  reset(): void {
    this.state.next({
      currentStep: 1,
      selectedCategory: null,
      cart: [],
      orderType: '',
      customerName: ''
    });
  }

  getSubtotal(): number {
    return this.state.value.cart.reduce((sum, item) => sum + item.totalPrice, 0);
  }

  getTax(): number {
    return 0;
  }

  getTotal(): number {
    return this.getSubtotal();
  }

  clearCart(): void {
    const current = this.state.value;
    this.state.next({ ...current, cart: [] });
  }

  // ============ متدهای جدید برای پرداخت ============

  sendToPos(amount: number): Observable<PosPaymentResponse> {
    return this.http.get<PosPaymentResponse>(`${this.apiUrl}/Pos/${amount}`);
  }

  getOnlinePaymentQr(amount: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/ZarinpalCallback/pay/${amount}`, {
      responseType: 'blob'
    });
  }

  async showOnlinePaymentQr(amount: number): Promise<string> {
    return new Promise((resolve, reject) => {
      this.getOnlinePaymentQr(amount).subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          resolve(url);
        },
        error: (error) => {
          console.error('Error generating QR code:', error);
          reject(error);
        }
      });
    });
  }

  createFullOrder(paymentMethodId?: number | null): Observable<any> {
    const current = this.state.value;
    const orderData: CreateOrderDto = {
      orderType: current.orderType,
      customerName: current.customerName,
      items: current.cart.map(item => ({
        productId: item.productId,
        quantity: item.quantity,
        price: item.price
      })),
      totalAmount: this.getTotal(),
      paymentMethod: '',
      paymentMethodId: paymentMethodId ?? null
    };
    return this.createOrder(orderData);
  }

  processPaymentWithPos(): Observable<any> {
    const total = this.getTotal();
    return new Observable(observer => {
      this.sendToPos(total).subscribe({
        next: (posResponse) => {
          if (posResponse.success || posResponse.isSuccess) {
            this.createFullOrder().subscribe({
              next: (order) => {
                this.reset();
                observer.next({ success: true, order });
                observer.complete();
              },
              error: (err) => {
                observer.error({ success: false, error: err, message: 'خطا در ثبت سفارش' });
              }
            });
          } else {
            observer.error({ success: false, message: 'پرداخت با POS ناموفق بود' });
          }
        },
        error: (err) => {
          observer.error({ success: false, error: err, message: 'خطا در ارتباط با دستگاه POS' });
        }
      });
    });
  }

  processPaymentWithOnline(): Observable<Blob> {
    const total = this.getTotal();
    return this.getOnlinePaymentQr(total);
  }

  processPaymentWithOnlineAndCreateOrder(): Observable<any> {
    const total = this.getTotal();
    return new Observable(observer => {
      this.getOnlinePaymentQr(total).subscribe({
        next: (qrBlob) => {
          this.createFullOrder().subscribe({
            next: (order) => {
              const qrUrl = URL.createObjectURL(qrBlob);
              this.reset();
              observer.next({ success: true, order, qrCodeUrl: qrUrl });
              observer.complete();
            },
            error: (err) => {
              observer.error({ success: false, error: err, message: 'خطا در ثبت سفارش' });
            }
          });
        },
        error: (err) => {
          observer.error({ success: false, error: err, message: 'خطا در ایجاد QR Code' });
        }
      });
    });
  }

  payWithOnlineAndStoreAuthority(): Observable<{ qrCodeUrl: string, orderId: number, authority: string }> {
    const total = this.getTotal();
    return new Observable(observer => {
      this.getOnlinePaymentQr(total).subscribe({
        next: (qrBlob) => {
          this.createFullOrder().subscribe({
            next: (order: any) => {
              const qrUrl = URL.createObjectURL(qrBlob);
              this.getPaymentAuthority(total).subscribe({
                next: (authResponse: any) => {
                  observer.next({
                    qrCodeUrl: qrUrl,
                    orderId: order.id || order.orderId,
                    authority: authResponse.authority
                  });
                  observer.complete();
                },
                error: (err) => {
                  console.warn('Could not get authority, using temporary value');
                  observer.next({
                    qrCodeUrl: qrUrl,
                    orderId: order.id || order.orderId,
                    authority: 'TEMPORARY_AUTHORITY'
                  });
                  observer.complete();
                }
              });
            },
            error: (err) => {
              observer.error({ success: false, error: err, message: 'خطا در ثبت سفارش' });
            }
          });
        },
        error: (err) => {
          observer.error({ success: false, error: err, message: 'خطا در ایجاد QR Code' });
        }
      });
    });
  }

  getPaymentAuthority(amount: number): Observable<{ authority: string }> {
    return this.http.get<{ authority: string }>(`${this.apiUrl}/ZarinpalCallback/get-authority/${amount}`);
  }

  checkPaymentStatus(orderId: number, authority: string, amount: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/PaymentVerify/check`, {
      orderId: orderId,
      authority: authority,
      amount: amount
    });
  }

  printReceipt(orderId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/Receipt/print/${orderId}`);
  }

  refreshQrCode(amount: number, orderId: number): Observable<Blob> {
    return this.getOnlinePaymentQr(amount);
  }

  getProducts() {
    return this.http.get(`${this.apiUrl}/Product`);
  }

  addProduct(product: any) {
    return this.http.post(`${this.apiUrl}/Product`, product);
  }

  updateProduct(product: any) {
    return this.http.put(`${this.apiUrl}/Product/${product.id}`, product);
  }

  deleteProduct(id: number) {
    return this.http.delete(`${this.apiUrl}/Product/${id}`);
  }

  uploadImage(file: File) {
    const formData = new FormData();
    formData.append("file", file);
    return this.http.post(`${this.apiUrl}/Product/upload`, formData);
  }

  addCategory(data: any) {
    return this.http.post(this.apiUrl + '/Category', data);
  }

  updateCategory(data: any) {
    return this.http.put(this.apiUrl + '/Category/' + data.id, data);
  }

  deleteCategory(id: number) {
    return this.http.delete(this.apiUrl + '/Category/' + id);
  }

  getOrdersByDateRange(startDate: string, endDate: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/order/date-range?startDate=${startDate}&endDate=${endDate}`);
  }

  getOrdersByDate(date: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/order/date/${date}`);
  }

  getOrdersByWeek(year: number, week: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/order/week/${year}/${week}`);
  }

  getOrdersByMonth(year: number, month: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/order/month/${year}/${month}`);
  }

  getOrdersByYear(year: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/order/year/${year}`);
  }

  getDefaultPosDevice(): Observable<any> {
    return this.http.get(`${this.apiUrl}/PosDevice/active`);
  }

  processPaymentWithPosByDevice(amount: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/PosDevice/pay`, { amount: amount });
  }

  checkDefaultPosConnection(): Observable<any> {
    return this.http.post(`${this.apiUrl}/PosDevice/check-connection`, {});
  }

  getReportStats(startDate: string, endDate: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/order/report-stats?startDate=${startDate}&endDate=${endDate}`);
  }

  getOrders() {
    return this.http.get<any[]>(this.apiUrl + '/Order');
  }

  printReport(orders: any[], reportTitle: string, reportPeriod: string, totalSales: number, totalTax: number, totalOrders: number, averageOrderValue: number, printDirectly: boolean = false): Observable<any> {
    const payload = {
      orders: orders,
      reportTitle: reportTitle,
      reportPeriod: reportPeriod,
      totalSales: totalSales,
      totalTax: totalTax,
      totalOrders: totalOrders,
      averageOrderValue: averageOrderValue,
      printDirectly: printDirectly
    };
    return this.http.post(`${this.apiUrl}/Receipt/print-report`, payload);
  }

  // ============ متدهای جدید برای تنظیمات انواع سفارش ============

  /**
   * دریافت تنظیمات انواع سفارش
   */
  getOrderTypeSettings(): Observable<any[]> {
    // ابتدا از localStorage میخوانیم
    const saved = localStorage.getItem('order_type_settings');
    if (saved) {
      return of(JSON.parse(saved));
    }
    
    // تنظیمات پیش‌فرض
    const defaultSettings = [
      {
        id: 'EatIn',
        name: 'EatIn',
        persianName: 'داخل سالن',
        icon: '🏠',
        active: true,
        priceFactor: 1,
        visibleCategoryIds: [],
        categoryPriceRules: []
      },
      {
        id: 'TakeAway',
        name: 'TakeAway',
        persianName: 'بیرون بر',
        icon: '🛍️',
        active: true,
        priceFactor: 1.1,
        visibleCategoryIds: [],
        categoryPriceRules: []
      }
    ];
    
    return of(defaultSettings);
  }

  /**
   * ذخیره تنظیمات انواع سفارش
   */
  saveOrderTypeSettings(settings: any[]): Observable<any> {
    // ذخیره در localStorage
    localStorage.setItem('order_type_settings', JSON.stringify(settings));
    
    // اگر سرور در دسترس است، به API هم ارسال کنید
    try {
      return this.http.post(`${this.apiUrl}/OrderTypeSettings`, settings);
    } catch (error) {
      return of({ success: true, message: 'تنظیمات به صورت محلی ذخیره شد', local: true });
    }
  }

  /**
   * دریافت محصولات با اعمال ضریب قیمت بر اساس نوع سفارش
   */
  getProductsWithPriceFactor(orderTypeId: string, categoryId?: number): Observable<any[]> {
    let url = `${this.apiUrl}/Product/with-price-factor/${orderTypeId}`;
    if (categoryId) {
      url += `?categoryId=${categoryId}`;
    }
    return this.http.get<any[]>(url);
  }

  /**
   * محاسبه قیمت نهایی یک محصول بر اساس نوع سفارش
   */
  calculateProductPrice(productId: number, orderTypeId: string, categoryId?: number, useSecondPrice: boolean = false): Observable<any> {
    return this.http.post(`${this.apiUrl}/OrderTypeSettings/calculate-price`, {
      productId,
      orderTypeId,
      categoryId,
      useSecondPrice
    });
  }

  /**
   * دریافت دسته‌بندی‌های قابل نمایش برای یک نوع سفارش خاص
   */
  getVisibleCategoriesForOrderType(orderTypeId: string): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.apiUrl}/OrderTypeSettings/${orderTypeId}/categories`);
  }

  /**
   * دریافت تنظیمات یک نوع سفارش خاص
   */
  getOrderTypeSettingById(orderTypeId: string): Observable<any> {
    const saved = localStorage.getItem('order_type_settings');
    if (saved) {
      const settings = JSON.parse(saved);
      const setting = settings.find((s: any) => s.id === orderTypeId);
      return of(setting);
    }
    return of(null);
  }

  /**
   * اعمال ضریب قیمت روی یک مقدار
   */
  applyPriceFactor(originalPrice: number, factor: number): number {
    return Math.round(originalPrice * factor * 100) / 100;
  }

  /**
   * دریافت ضریب قیمت برای یک دسته‌بندی خاص
   */
  getCategoryPriceFactor(orderTypeId: string, categoryId: string): number {
    const saved = localStorage.getItem('order_type_settings');
    if (saved) {
      const settings = JSON.parse(saved);
      const orderType = settings.find((s: any) => s.id === orderTypeId);
      if (orderType) {
        const rule = orderType.categoryPriceRules?.find((r: any) => r.categoryId === categoryId);
        if (rule) {
          return rule.factor;
        }
        return orderType.priceFactor || 1;
      }
    }
    return 1;
  }
}