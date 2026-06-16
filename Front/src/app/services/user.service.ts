import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AdminUser {
  id: number;
  username: string;
  fullName: string;
  isSuperAdmin: boolean;
  isActive: boolean;
  createdAt: string;
  permissions: string[];
}

export interface AdminUserDto {
  id?: number;
  username: string;
  fullName: string;
  password?: string;
  isSuperAdmin: boolean;
  isActive: boolean;
  permissions: string[];
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = 'http://localhost:5000/api/user';

  constructor(private http: HttpClient) {}

  getSections(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/sections`);
  }

  getUsers(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(this.apiUrl);
  }

  createUser(dto: AdminUserDto): Observable<any> {
    return this.http.post(this.apiUrl, dto);
  }

  updateUser(id: number, dto: AdminUserDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, dto);
  }

  deleteUser(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
