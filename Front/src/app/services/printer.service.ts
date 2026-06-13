import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PrinterSetting } from '../models/printer.model';

@Injectable({
  providedIn: 'root'
})
export class PrinterService {
  private apiUrl = 'http://localhost:5000/api/printer';

  constructor(private http: HttpClient) {}

  getPrinters(): Observable<PrinterSetting[]> {
    return this.http.get<PrinterSetting[]>(this.apiUrl);
  }

  getActivePrinters(): Observable<PrinterSetting[]> {
    return this.http.get<PrinterSetting[]>(`${this.apiUrl}/active`);
  }

  getPrintersByType(type: string): Observable<PrinterSetting[]> {
    return this.http.get<PrinterSetting[]>(`${this.apiUrl}/type/${type}`);
  }

  getPrinter(id: number): Observable<PrinterSetting> {
    return this.http.get<PrinterSetting>(`${this.apiUrl}/${id}`);
  }

  createPrinter(printer: PrinterSetting): Observable<any> {
    return this.http.post(this.apiUrl, printer);
  }

  updatePrinter(id: number, printer: PrinterSetting): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, printer);
  }

  deletePrinter(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  testPrinter(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/test/${id}`, {});
  }

  getInstalledPrinters(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/installed`);
  }
}