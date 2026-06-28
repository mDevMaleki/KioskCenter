import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Budget {
  id: number;
  accountId: number;
  accountCode: string;
  accountName: string;
  periodStart: string;
  periodEnd: string;
  budgetedAmount: number;
  note?: string | null;
}

export interface BudgetDto {
  accountId: number;
  periodStart: string;
  periodEnd: string;
  budgetedAmount: number;
  note?: string | null;
}

export interface BudgetVsActual {
  id: number;
  accountId: number;
  accountCode: string;
  accountName: string;
  periodStart: string;
  periodEnd: string;
  budgetedAmount: number;
  actualAmount: number;
  variance: number;
  variancePercent: number;
}

@Injectable({ providedIn: 'root' })
export class BudgetService {
  private apiUrl = 'http://localhost:5000/api/budget';

  constructor(private http: HttpClient) {}

  getAll(periodStart?: string, periodEnd?: string): Observable<Budget[]> {
    const params: string[] = [];
    if (periodStart) params.push(`periodStart=${periodStart}`);
    if (periodEnd) params.push(`periodEnd=${periodEnd}`);
    const qs = params.length ? '?' + params.join('&') : '';
    return this.http.get<Budget[]>(`${this.apiUrl}${qs}`);
  }

  create(dto: BudgetDto): Observable<Budget> {
    return this.http.post<Budget>(this.apiUrl, dto);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  getVsActual(periodStart: string, periodEnd: string): Observable<BudgetVsActual[]> {
    return this.http.get<BudgetVsActual[]>(`${this.apiUrl}/vs-actual?periodStart=${periodStart}&periodEnd=${periodEnd}`);
  }
}
