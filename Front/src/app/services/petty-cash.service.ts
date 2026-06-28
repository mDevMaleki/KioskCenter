import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PettyCashFund {
  id: number;
  name: string;
  custodian?: string | null;
  balance: number;
  sourceCashAccountId: number;
  sourceCashAccountName?: string | null;
}

export interface PettyCashFundDto {
  name: string;
  custodian?: string | null;
  sourceCashAccountId: number;
}

export interface PettyCashTransactionItem {
  id: number;
  type: string;
  amount: number;
  description?: string | null;
  transactionDate: string;
  expenseAccountName?: string | null;
}

@Injectable({ providedIn: 'root' })
export class PettyCashService {
  private apiUrl = 'http://localhost:5000/api/pettycash';

  constructor(private http: HttpClient) {}

  getFunds(): Observable<PettyCashFund[]> {
    return this.http.get<PettyCashFund[]>(`${this.apiUrl}/funds`);
  }

  createFund(dto: PettyCashFundDto): Observable<PettyCashFund> {
    return this.http.post<PettyCashFund>(`${this.apiUrl}/funds`, dto);
  }

  getTransactions(fundId: number): Observable<PettyCashTransactionItem[]> {
    return this.http.get<PettyCashTransactionItem[]>(`${this.apiUrl}/funds/${fundId}/transactions`);
  }

  replenish(fundId: number, amount: number, description?: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/funds/${fundId}/replenish`, { amount, description });
  }

  spend(fundId: number, amount: number, expenseAccountId: number, description?: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/funds/${fundId}/spend`, { amount, expenseAccountId, description });
  }
}
