import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PurchaseInvoiceItemRequest {
  rawMaterialId: number;
  unitId: number;
  quantity: number;
  unitPrice: number;
}

export interface PurchaseInvoiceRequest {
  partyId: number;
  note?: string;
  items: PurchaseInvoiceItemRequest[];
}

export interface PurchaseInvoiceListItem {
  id: number;
  partyId: number;
  partyName: string;
  totalAmount: number;
  note?: string | null;
  createdAt: string;
}

export interface PurchaseInvoiceDetailItem {
  id: number;
  rawMaterialId: number;
  rawMaterialName: string;
  unitId: number;
  unitName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface PurchaseInvoiceDetail extends PurchaseInvoiceListItem {
  items: PurchaseInvoiceDetailItem[];
}

@Injectable({
  providedIn: 'root'
})
export class PurchaseInvoiceService {
  private apiUrl = 'http://localhost:5000/api/purchaseinvoice';

  constructor(private http: HttpClient) {}

  getAll(partyId?: number, page = 1, pageSize = 50): Observable<{ total: number; page: number; pageSize: number; items: PurchaseInvoiceListItem[] }> {
    let url = `${this.apiUrl}?page=${page}&pageSize=${pageSize}`;
    if (partyId) url += `&partyId=${partyId}`;
    return this.http.get<{ total: number; page: number; pageSize: number; items: PurchaseInvoiceListItem[] }>(url);
  }

  getOne(id: number): Observable<PurchaseInvoiceDetail> {
    return this.http.get<PurchaseInvoiceDetail>(`${this.apiUrl}/${id}`);
  }

  create(request: PurchaseInvoiceRequest): Observable<any> {
    return this.http.post(this.apiUrl, request);
  }
}
