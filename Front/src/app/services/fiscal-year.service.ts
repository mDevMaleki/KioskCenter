import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface FiscalYear {
  id: number;
  name: string;
  startDate: string;
  endDate: string;
  isClosed: boolean;
  closedAt?: string | null;
  createdAt: string;
}

export interface FiscalYearDto {
  name: string;
  startDate: string;
  endDate: string;
}

@Injectable({ providedIn: 'root' })
export class FiscalYearService {
  private apiUrl = 'http://localhost:5000/api/fiscalyear';

  constructor(private http: HttpClient) {}

  getAll(): Observable<FiscalYear[]> {
    return this.http.get<FiscalYear[]>(this.apiUrl);
  }

  getCurrent(): Observable<any> {
    return this.http.get(`${this.apiUrl}/current`);
  }

  create(dto: FiscalYearDto): Observable<FiscalYear> {
    return this.http.post<FiscalYear>(this.apiUrl, dto);
  }

  close(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/close`, {});
  }

  reopen(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/reopen`, {});
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
