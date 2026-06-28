import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface FixedAsset {
  id: number;
  name: string;
  purchaseDate: string;
  purchaseValue: number;
  salvageValue: number;
  usefulLifeMonths: number;
  accumulatedDepreciation: number;
  bookValue: number;
  assetAccountId: number;
  depreciationExpenseAccountId: number;
  accumulatedDepreciationAccountId: number;
}

export interface FixedAssetDto {
  name: string;
  purchaseDate: string;
  purchaseValue: number;
  salvageValue: number;
  usefulLifeMonths: number;
  assetAccountId: number;
  depreciationExpenseAccountId: number;
  accumulatedDepreciationAccountId: number;
}

export interface DepreciationRecord {
  id: number;
  fixedAssetId: number;
  periodDate: string;
  amount: number;
  journalEntryId?: number | null;
}

@Injectable({ providedIn: 'root' })
export class FixedAssetService {
  private apiUrl = 'http://localhost:5000/api/fixedasset';

  constructor(private http: HttpClient) {}

  getAll(): Observable<FixedAsset[]> {
    return this.http.get<FixedAsset[]>(this.apiUrl);
  }

  getHistory(id: number): Observable<DepreciationRecord[]> {
    return this.http.get<DepreciationRecord[]>(`${this.apiUrl}/${id}/history`);
  }

  create(dto: FixedAssetDto): Observable<FixedAsset> {
    return this.http.post<FixedAsset>(this.apiUrl, dto);
  }

  update(id: number, dto: FixedAssetDto): Observable<FixedAsset> {
    return this.http.put<FixedAsset>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  runDepreciation(id: number, periodDate: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/run-depreciation`, { periodDate });
  }

  runDepreciationAll(periodDate: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/run-depreciation-all`, { periodDate });
  }
}
