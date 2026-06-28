import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface UnitOfMeasure {
  id: number;
  name: string;
  baseUnitId: number | null;
  baseUnitName?: string | null;
  conversionFactor: number;
}

export interface UnitOfMeasureDto {
  name: string;
  baseUnitId: number | null;
  conversionFactor: number;
}

@Injectable({
  providedIn: 'root'
})
export class UnitService {
  private apiUrl = 'http://localhost:5000/api/unitofmeasure';

  constructor(private http: HttpClient) {}

  getAll(): Observable<UnitOfMeasure[]> {
    return this.http.get<UnitOfMeasure[]>(this.apiUrl);
  }

  create(dto: UnitOfMeasureDto): Observable<UnitOfMeasure> {
    return this.http.post<UnitOfMeasure>(this.apiUrl, dto);
  }

  update(id: number, dto: UnitOfMeasureDto): Observable<UnitOfMeasure> {
    return this.http.put<UnitOfMeasure>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
