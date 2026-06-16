import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ExpenseItem {
  id: number;
  date: string;
  description?: string | null;
  amount: number;
  accountId: number;
  accountName?: string | null;
  cashAccountId: number;
  cashAccountName?: string | null;
  journalEntryId: number;
}

export interface ExpenseList {
  total: number;
  totalAmount: number;
  page: number;
  pageSize: number;
  items: ExpenseItem[];
}

export interface ExpenseDto {
  date: string;
  description?: string | null;
  amount: number;
  accountId: number;
  cashAccountId: number;
}

@Injectable({
  providedIn: 'root'
})
export class ExpenseService {
  private apiUrl = 'http://localhost:5000/api/expense';

  constructor(private http: HttpClient) {}

  getAll(from?: string, to?: string, page = 1, pageSize = 50): Observable<ExpenseList> {
    let url = `${this.apiUrl}?page=${page}&pageSize=${pageSize}`;
    if (from) url += `&from=${from}`;
    if (to) url += `&to=${to}`;
    return this.http.get<ExpenseList>(url);
  }

  create(dto: ExpenseDto): Observable<any> {
    return this.http.post(this.apiUrl, dto);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
