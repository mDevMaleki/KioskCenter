import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CashierCartItem {
  productId: number;
  productName: string;
  unitPrice: number;
  quantity: number;
}

export interface ParkedOrderItem {
  id: number;
  productId: number;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  product?: { name: string };
}

export interface ParkedOrder {
  id: number;
  orderNumber: number;
  orderDate: string;
  orderType: string;
  customerName?: string;
  parkedLabel?: string;
  totalAmount: number;
  orderItems: ParkedOrderItem[];
}

@Injectable({ providedIn: 'root' })
export class CashierService {
  private apiUrl = 'http://localhost:5000/api/order';

  constructor(private http: HttpClient) {}

  createOrder(payload: {
    customerName?: string;
    orderType: string;
    items: { productId: number; quantity: number }[];
    paymentMethodId?: number | null;
    park?: boolean;
    parkedLabel?: string;
  }): Observable<any> {
    // سفارش‌های صندوق فروشگاهی همیشه با منبع Cashier ثبت می‌شوند تا پرینتر مناسب انتخاب شود
    return this.http.post(`${this.apiUrl}/create`, { ...payload, source: 'Cashier' });
  }

  confirmPayment(orderId: number, paymentMethodId?: number | null): Observable<any> {
    return this.http.post(`${this.apiUrl}/confirm/${orderId}`, paymentMethodId ? { paymentMethodId } : {});
  }

  getParkedOrders(): Observable<ParkedOrder[]> {
    return this.http.get<ParkedOrder[]>(`${this.apiUrl}/parked`);
  }

  deleteOrder(orderId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${orderId}`);
  }

  printReceipt(orderId: number): Observable<any> {
    return this.http.get(`${this.apiUrl.replace('/order', '')}/Receipt/print/${orderId}`);
  }
}
