import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { JournalEntryRefType } from './journal-entry.service';

export enum AccountType {
  Asset = 'Asset',
  Liability = 'Liability',
  Equity = 'Equity',
  Revenue = 'Revenue',
  Expense = 'Expense'
}

export interface Account {
  id: number;
  code: string;
  name: string;
  type: AccountType;
  parentId?: number | null;
  isGroup: boolean;
  isActive: boolean;
  balance: number;
}

export interface AccountDto {
  code: string;
  name: string;
  type: AccountType;
  parentId?: number | null;
  isGroup: boolean;
  isActive: boolean;
}

export interface AccountLedgerItem {
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
  cashAccountName?: string | null;
}

export interface AccountLedger {
  accountId: number;
  accountCode: string;
  accountName: string;
  balance: number;
  total: number;
  page: number;
  pageSize: number;
  items: AccountLedgerItem[];
}

export interface TrialBalanceItem {
  id: number;
  code: string;
  name: string;
  type: AccountType;
  parentId?: number | null;
  isGroup: boolean;
  debit: number;
  credit: number;
}

export interface TrialBalance {
  items: TrialBalanceItem[];
  totalDebit: number;
  totalCredit: number;
}

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private apiUrl = 'http://localhost:5000/api/account';

  constructor(private http: HttpClient) {}

  getAll(search?: string, type?: AccountType): Observable<Account[]> {
    let url = `${this.apiUrl}?`;
    const params: string[] = [];
    if (search) params.push(`search=${encodeURIComponent(search)}`);
    if (type) params.push(`type=${type}`);
    return this.http.get<Account[]>(url + params.join('&'));
  }

  getOne(id: number): Observable<Account> {
    return this.http.get<Account>(`${this.apiUrl}/${id}`);
  }

  create(dto: AccountDto): Observable<Account> {
    return this.http.post<Account>(this.apiUrl, dto);
  }

  update(id: number, dto: AccountDto): Observable<Account> {
    return this.http.put<Account>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  getLedger(id: number, page = 1, pageSize = 50): Observable<AccountLedger> {
    return this.http.get<AccountLedger>(`${this.apiUrl}/${id}/ledger?page=${page}&pageSize=${pageSize}`);
  }

  getTrialBalance(): Observable<TrialBalance> {
    return this.http.get<TrialBalance>(`${this.apiUrl}/trial-balance`);
  }
}
