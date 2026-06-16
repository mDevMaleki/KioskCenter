import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface InventoryProduct {
  id: number;
  name: string;
  categoryId: number;
  categoryName: string | null;
  imageUrl: string;
  stockQuantity: number;
  minStockLevel: number;
  unit: string;
}

export interface InventoryTransaction {
  id: number;
  productId: number;
  productName: string | null;
  type: 'In' | 'Out' | 'Adjustment';
  quantity: number;
  stockAfter: number;
  unitPrice: number | null;
  note: string | null;
  createdAt: string;
}

export interface InventoryTransactionsResult {
  total: number;
  page: number;
  pageSize: number;
  items: InventoryTransaction[];
}

@Injectable({
  providedIn: 'root'
})
export class InventoryService {
  private apiUrl = 'http://localhost:5000/api/inventory';

  constructor(private http: HttpClient) {}

  getProductsStock(filters?: { categoryId?: number; search?: string; lowStockOnly?: boolean }): Observable<InventoryProduct[]> {
    let params = new HttpParams();
    if (filters?.categoryId) params = params.set('categoryId', filters.categoryId);
    if (filters?.search) params = params.set('search', filters.search);
    if (filters?.lowStockOnly) params = params.set('lowStockOnly', filters.lowStockOnly);
    return this.http.get<InventoryProduct[]>(`${this.apiUrl}/products`, { params });
  }

  getTransactions(filters?: { productId?: number; type?: string; page?: number; pageSize?: number }): Observable<InventoryTransactionsResult> {
    let params = new HttpParams();
    if (filters?.productId) params = params.set('productId', filters.productId);
    if (filters?.type) params = params.set('type', filters.type);
    if (filters?.page) params = params.set('page', filters.page);
    if (filters?.pageSize) params = params.set('pageSize', filters.pageSize);
    return this.http.get<InventoryTransactionsResult>(`${this.apiUrl}/transactions`, { params });
  }

  stockIn(data: { productId: number; quantity: number; unitPrice?: number | null; note?: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/stock-in`, data);
  }

  stockOut(data: { productId: number; quantity: number; note?: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/stock-out`, data);
  }

  adjustStock(data: { productId: number; newQuantity: number; note?: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/adjust`, data);
  }

  updateSettings(productId: number, data: { minStockLevel?: number; unit?: string }): Observable<any> {
    return this.http.put(`${this.apiUrl}/settings/${productId}`, data);
  }
}
