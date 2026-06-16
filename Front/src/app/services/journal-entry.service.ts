import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export enum JournalEntryRefType {
  Manual = 'Manual',
  PurchaseInvoice = 'PurchaseInvoice',
  SaleInvoice = 'SaleInvoice',
  Payment = 'Payment',
  Receipt = 'Receipt',
  Expense = 'Expense'
}

export interface JournalEntrySummary {
  id: number;
  number: number;
  entryDate: string;
  description?: string | null;
  refType: JournalEntryRefType;
  refId?: number | null;
  totalDebit: number;
  totalCredit: number;
}

export interface JournalEntryLineDetail {
  id: number;
  accountId: number;
  accountCode?: string | null;
  accountName?: string | null;
  debit: number;
  credit: number;
  description?: string | null;
  partyId?: number | null;
  partyName?: string | null;
  cashAccountId?: number | null;
  cashAccountName?: string | null;
}

export interface JournalEntryDetail {
  id: number;
  number: number;
  entryDate: string;
  description?: string | null;
  refType: JournalEntryRefType;
  refId?: number | null;
  lines: JournalEntryLineDetail[];
}

export interface JournalEntryLineDto {
  accountId: number;
  debit: number;
  credit: number;
  description?: string | null;
  partyId?: number | null;
  cashAccountId?: number | null;
}

export interface JournalEntryDto {
  entryDate: string;
  description?: string | null;
  lines: JournalEntryLineDto[];
}

export interface JournalEntryList {
  total: number;
  page: number;
  pageSize: number;
  items: JournalEntrySummary[];
}

@Injectable({
  providedIn: 'root'
})
export class JournalEntryService {
  private apiUrl = 'http://localhost:5000/api/journalentry';

  constructor(private http: HttpClient) {}

  getAll(from?: string, to?: string, refType?: JournalEntryRefType, page = 1, pageSize = 50): Observable<JournalEntryList> {
    let url = `${this.apiUrl}?page=${page}&pageSize=${pageSize}`;
    if (from) url += `&from=${from}`;
    if (to) url += `&to=${to}`;
    if (refType !== undefined && refType !== null) url += `&refType=${refType}`;
    return this.http.get<JournalEntryList>(url);
  }

  getOne(id: number): Observable<JournalEntryDetail> {
    return this.http.get<JournalEntryDetail>(`${this.apiUrl}/${id}`);
  }

  create(dto: JournalEntryDto): Observable<any> {
    return this.http.post(this.apiUrl, dto);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
