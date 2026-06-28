import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PaymentMethod {
  id: number;
  name: string; // "Cash", "Pos", "Online"
  title: string;
  icon: string;
  description: string;
  isActive: boolean;
  sortOrder: number;
  requiresPosDevice: boolean;
  // صندوق/بانکی که با این روش پرداخت بدهکار می‌شود
  cashAccountId?: number | null;
  // این فیلدها از سرور می‌آیند ولی در زمان ایجاد نیازی به مقداردهی ندارند
  createdAt?: Date;
  updatedAt?: Date;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentMethodService {
  private apiUrl = 'http://localhost:5000/api/PaymentMethod';

  constructor(private http: HttpClient) {}

  getActiveMethods(): Observable<PaymentMethod[]> {
    return this.http.get<PaymentMethod[]>(`${this.apiUrl}/active`);
  }

  getAllMethods(): Observable<PaymentMethod[]> {
    return this.http.get<PaymentMethod[]>(this.apiUrl);
  }

  getMethod(id: number): Observable<PaymentMethod> {
    return this.http.get<PaymentMethod>(`${this.apiUrl}/${id}`);
  }

  createMethod(method: PaymentMethod): Observable<any> {
    // حذف فیلدهای createdAt و updatedAt قبل از ارسال به سرور
    const { createdAt, updatedAt, ...methodToSend } = method;
    return this.http.post(this.apiUrl, methodToSend);
  }

  updateMethod(id: number, method: PaymentMethod): Observable<any> {
    // حذف فیلدهای createdAt و updatedAt قبل از ارسال به سرور
    const { createdAt, updatedAt, ...methodToSend } = method;
    return this.http.put(`${this.apiUrl}/${id}`, methodToSend);
  }

  deleteMethod(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  seedMethods(): Observable<any> {
    return this.http.post(`${this.apiUrl}/seed`, {});
  }
}