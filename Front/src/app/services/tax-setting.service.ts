import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TaxSetting {
  id: number;
  vatRate: number;
  isEnabled: boolean;
}

@Injectable({ providedIn: 'root' })
export class TaxSettingService {
  private apiUrl = 'http://localhost:5000/api/taxsetting';

  constructor(private http: HttpClient) {}

  get(): Observable<TaxSetting> {
    return this.http.get<TaxSetting>(this.apiUrl);
  }

  update(vatRate: number, isEnabled: boolean): Observable<TaxSetting> {
    return this.http.put<TaxSetting>(this.apiUrl, { vatRate, isEnabled });
  }
}
