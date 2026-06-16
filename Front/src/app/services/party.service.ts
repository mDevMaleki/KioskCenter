import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export enum PartyType {
  Supplier = 1,
  Customer = 2,
  Both = 3
}

export enum PartyTransactionType {
  PurchaseInvoice = 1,
  SaleInvoice = 2,
  Payment = 3,
  Receipt = 4
}

export interface Party {
  id: number;
  name: string;
  type: PartyType;
  phone?: string | null;
  address?: string | null;
  balance: number;
  createdAt: string;
}

export interface PartyDto {
  name: string;
  type: PartyType;
  phone?: string | null;
  address?: string | null;
}

export interface PartyTransaction {
  id: number;
  type: PartyTransactionType;
  amount: number;
  balanceAfter: number;
  refId?: number | null;
  description?: string | null;
  createdAt: string;
}

export interface PartyLedger {
  partyId: number;
  partyName: string;
  balance: number;
  total: number;
  page: number;
  pageSize: number;
  items: PartyTransaction[];
}

export interface PartyCashRequest {
  amount: number;
  description?: string;
  cashAccountId: number;
}

@Injectable({
  providedIn: 'root'
})
export class PartyService {
  private apiUrl = 'http://localhost:5000/api/party';

  constructor(private http: HttpClient) {}

  getAll(search?: string, type?: PartyType): Observable<Party[]> {
    let url = `${this.apiUrl}?`;
    const params: string[] = [];
    if (search) params.push(`search=${encodeURIComponent(search)}`);
    if (type) params.push(`type=${type}`);
    return this.http.get<Party[]>(url + params.join('&'));
  }

  getOne(id: number): Observable<Party> {
    return this.http.get<Party>(`${this.apiUrl}/${id}`);
  }

  create(dto: PartyDto): Observable<Party> {
    return this.http.post<Party>(this.apiUrl, dto);
  }

  update(id: number, dto: PartyDto): Observable<Party> {
    return this.http.put<Party>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  getLedger(id: number, page = 1, pageSize = 50): Observable<PartyLedger> {
    return this.http.get<PartyLedger>(`${this.apiUrl}/${id}/ledger?page=${page}&pageSize=${pageSize}`);
  }

  payment(id: number, request: PartyCashRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/payment`, request);
  }

  receipt(id: number, request: PartyCashRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/receipt`, request);
  }
}
