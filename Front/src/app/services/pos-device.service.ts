import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PosDevice, PosPayRequest, PosPaymentResponse, PosConnectionResponse } from '../models/pos-device.model';

@Injectable({
  providedIn: 'root'
})
export class PosDeviceService {
  private apiUrl = 'http://localhost:5000/api/PosDevice';

  constructor(private http: HttpClient) {}

  // دریافت لیست دستگاه‌ها
  getDevices(): Observable<PosDevice[]> {
    return this.http.get<PosDevice[]>(this.apiUrl);
  }

  // دریافت یک دستگاه
  getDevice(id: number): Observable<PosDevice> {
    return this.http.get<PosDevice>(`${this.apiUrl}/${id}`);
  }

  // اضافه کردن دستگاه
  addDevice(device: PosDevice): Observable<any> {
    return this.http.post(this.apiUrl, device);
  }

  // ویرایش دستگاه
  updateDevice(id: number, device: PosDevice): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, device);
  }

  // حذف دستگاه
  deleteDevice(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  // انجام پرداخت
  pay(request: PosPayRequest): Observable<PosPaymentResponse> {
    return this.http.post<PosPaymentResponse>(`${this.apiUrl}/pay`, request);
  }

  // بررسی اتصال
  checkConnection(deviceId?: number): Observable<PosConnectionResponse> {
    return this.http.post<PosConnectionResponse>(`${this.apiUrl}/check-connection`, { deviceId });
  }

  // دریافت دستگاه فعال
  getActiveDevice(): Observable<PosDevice> {
    return this.http.get<PosDevice>(`${this.apiUrl}/active`);
  }
}