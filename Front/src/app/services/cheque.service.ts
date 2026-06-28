import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export enum ChequeDirection {
  Received = 'Received',
  Paid = 'Paid'
}

export enum ChequeStatus {
  InHand = 'InHand',
  Deposited = 'Deposited',
  Cleared = 'Cleared',
  Bounced = 'Bounced',
  Returned = 'Returned'
}

export interface Cheque {
  id: number;
  number: string;
  bankName?: string | null;
  issueDate: string;
  dueDate: string;
  amount: number;
  direction: ChequeDirection;
  status: ChequeStatus;
  partyId: number;
  partyName?: string | null;
  cashAccountId?: number | null;
  cashAccountName?: string | null;
  description?: string | null;
}

export interface ChequeDto {
  number: string;
  bankName?: string | null;
  issueDate: string;
  dueDate: string;
  amount: number;
  direction: ChequeDirection;
  partyId: number;
  description?: string | null;
}

@Injectable({ providedIn: 'root' })
export class ChequeService {
  private apiUrl = 'http://localhost:5000/api/cheque';

  constructor(private http: HttpClient) {}

  getAll(direction?: ChequeDirection, status?: ChequeStatus, partyId?: number): Observable<Cheque[]> {
    const params: string[] = [];
    if (direction) params.push(`direction=${direction}`);
    if (status) params.push(`status=${status}`);
    if (partyId) params.push(`partyId=${partyId}`);
    const qs = params.length ? '?' + params.join('&') : '';
    return this.http.get<Cheque[]>(`${this.apiUrl}${qs}`);
  }

  getDueSoon(days = 7): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/due-soon?days=${days}`);
  }

  create(dto: ChequeDto): Observable<Cheque> {
    return this.http.post<Cheque>(this.apiUrl, dto);
  }

  deposit(id: number, cashAccountId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/deposit`, { cashAccountId });
  }

  clear(id: number, cashAccountId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/clear`, { cashAccountId });
  }

  bounce(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/bounce`, {});
  }

  returnCheque(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/return`, {});
  }
}
