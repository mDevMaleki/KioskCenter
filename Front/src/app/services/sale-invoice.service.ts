import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SaleInvoiceItemRequest {
  productId: number;
  quantity: number;
  unitPrice: number;
}

export interface SaleInvoiceRequest {
  partyId: number;
  note?: string;
  items: SaleInvoiceItemRequest[];
}

export interface SaleInvoiceListItem {
  id: number;
  partyId: number;
  partyName: string;
  totalAmount: number;
  note?: string | null;
  createdAt: string;
}

export interface SaleInvoiceDetailItem {
  id: number;
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface SaleInvoiceDetail extends SaleInvoiceListItem {
  items: SaleInvoiceDetailItem[];
}

@Injectable({
  providedIn: 'root'
})
export class SaleInvoiceService {
  private apiUrl = 'http://localhost:5000/api/saleinvoice';

  constructor(private http: HttpClient) {}

  getAll(partyId?: number, page = 1, pageSize = 50): Observable<{ total: number; page: number; pageSize: number; items: SaleInvoiceListItem[] }> {
    let url = `${this.apiUrl}?page=${page}&pageSize=${pageSize}`;
    if (partyId) url += `&partyId=${partyId}`;
    return this.http.get<{ total: number; page: number; pageSize: number; items: SaleInvoiceListItem[] }>(url);
  }

  getOne(id: number): Observable<SaleInvoiceDetail> {
    return this.http.get<SaleInvoiceDetail>(`${this.apiUrl}/${id}`);
  }

  create(request: SaleInvoiceRequest): Observable<any> {
    return this.http.post(this.apiUrl, request);
  }
}
