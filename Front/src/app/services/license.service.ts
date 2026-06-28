import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface LicenseStatus {
  licensed: boolean;
  message: string;
  hardwareHash: string;
}

@Injectable({
  providedIn: 'root'
})
export class LicenseService {
  private apiUrl = 'http://localhost:5000/api/info';

  constructor(private http: HttpClient) {}

  getStatus(): Observable<LicenseStatus> {
    return this.http.get<LicenseStatus>(`${this.apiUrl}/license-status`);
  }

  uploadLicense(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/upload-license`, formData);
  }
}
