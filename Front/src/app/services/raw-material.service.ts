import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RawMaterial {
  id: number;
  name: string;
  unitId: number;
  unitName: string;
  stockQuantity: number;
  minStockLevel: number;
  createdAt: string;
}

export interface RawMaterialDto {
  name: string;
  unitId: number;
  minStockLevel: number;
}

export interface RawMaterialTradeRequest {
  rawMaterialId: number;
  quantity: number;
  unitPrice?: number | null;
  totalPrice?: number | null;
  partyName?: string;
  note?: string;
}

export interface AdjustRawMaterialRequest {
  rawMaterialId: number;
  newQuantity: number;
  note?: string;
}

export enum RawMaterialTransactionType {
  In = 1,
  Out = 2,
  Adjustment = 3
}

export interface RawMaterialTransaction {
  id: number;
  rawMaterialId: number;
  rawMaterialName: string;
  unit: string;
  type: RawMaterialTransactionType;
  quantity: number;
  stockAfter: number;
  unitPrice?: number | null;
  totalPrice?: number | null;
  partyName?: string;
  note?: string;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class RawMaterialService {
  private apiUrl = 'http://localhost:5000/api/rawmaterial';

  constructor(private http: HttpClient) {}

  getAll(search?: string, lowStockOnly = false): Observable<RawMaterial[]> {
    let url = `${this.apiUrl}?lowStockOnly=${lowStockOnly}`;
    if (search) url += `&search=${encodeURIComponent(search)}`;
    return this.http.get<RawMaterial[]>(url);
  }

  create(dto: RawMaterialDto): Observable<RawMaterial> {
    return this.http.post<RawMaterial>(this.apiUrl, dto);
  }

  update(id: number, dto: RawMaterialDto): Observable<RawMaterial> {
    return this.http.put<RawMaterial>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  consume(request: RawMaterialTradeRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/consume`, request);
  }

  adjust(request: AdjustRawMaterialRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/adjust`, request);
  }

  getTransactions(rawMaterialId?: number, page = 1, pageSize = 50): Observable<{ total: number; page: number; pageSize: number; items: RawMaterialTransaction[] }> {
    let url = `${this.apiUrl}/transactions?page=${page}&pageSize=${pageSize}`;
    if (rawMaterialId) url += `&rawMaterialId=${rawMaterialId}`;
    return this.http.get<{ total: number; page: number; pageSize: number; items: RawMaterialTransaction[] }>(url);
  }
}
