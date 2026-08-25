import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface MoadianSettings {
  id: number;
  isEnabled: boolean;
  memoryId?: string | null;
  sellerEconomicCode?: string | null;
  apiUrl: string;
  lastUpdatedAt?: string | null;
  hasKeys: boolean;
}

export interface MoadianSettingsDto {
  isEnabled: boolean;
  memoryId?: string;
  sellerEconomicCode?: string;
  privateKeyPem?: string;
  certificatePem?: string;
  apiUrl?: string;
}

@Injectable({ providedIn: 'root' })
export class MoadianService {
  private apiUrl = 'http://localhost:5000/api/moadian';

  constructor(private http: HttpClient) {}

  getSettings(): Observable<MoadianSettings> {
    return this.http.get<MoadianSettings>(`${this.apiUrl}/settings`);
  }

  updateSettings(dto: MoadianSettingsDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/settings`, dto);
  }

  sendInvoice(saleInvoiceId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/send/${saleInvoiceId}`, {});
  }
}
