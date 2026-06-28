import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { JournalEntryRefType } from './journal-entry.service';

export enum CashAccountType {
  Cash = 'Cash',
  Bank = 'Bank'
}

export interface CashAccount {
  id: number;
  name: string;
  type: CashAccountType;
  accountNumber?: string | null;
  accountId: number;
  balance: number;
  isActive: boolean;
  createdAt: string;
}

export interface CashAccountDto {
  name: string;
  type: CashAccountType;
  accountNumber?: string | null;
  accountId: number;
  isActive: boolean;
}

export interface CashAccountLedgerItem {
  id: number;
  journalEntryId: number;
  journalEntryNumber: number;
  entryDate: string;
  refType: JournalEntryRefType;
  refId?: number | null;
  debit: number;
  credit: number;
  description?: string | null;
  partyName?: string | null;
}

export interface CashAccountLedger {
  cashAccountId: number;
  cashAccountName: string;
  balance: number;
  total: number;
  page: number;
  pageSize: number;
  items: CashAccountLedgerItem[];
}

@Injectable({
  providedIn: 'root'
})
export class CashAccountService {
  private apiUrl = 'http://localhost:5000/api/cashaccount';

  constructor(private http: HttpClient) {}

  getAll(activeOnly?: boolean): Observable<CashAccount[]> {
    let url = `${this.apiUrl}?`;
    if (activeOnly) url += `activeOnly=true`;
    return this.http.get<CashAccount[]>(url);
  }

  getOne(id: number): Observable<CashAccount> {
    return this.http.get<CashAccount>(`${this.apiUrl}/${id}`);
  }

  create(dto: CashAccountDto): Observable<CashAccount> {
    return this.http.post<CashAccount>(this.apiUrl, dto);
  }

  update(id: number, dto: CashAccountDto): Observable<CashAccount> {
    return this.http.put<CashAccount>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  getLedger(id: number, page = 1, pageSize = 50): Observable<CashAccountLedger> {
    return this.http.get<CashAccountLedger>(`${this.apiUrl}/${id}/ledger?page=${page}&pageSize=${pageSize}`);
  }
}
