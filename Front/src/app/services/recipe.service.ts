import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RecipeItem {
  id?: number;
  rawMaterialId: number;
  rawMaterialName?: string;
  unit?: string;
  quantity: number;
}

export interface ProductRecipe {
  productId: number;
  productName: string;
  items: RecipeItem[];
}

@Injectable({
  providedIn: 'root'
})
export class RecipeService {
  private apiUrl = 'http://localhost:5000/api/recipe';

  constructor(private http: HttpClient) {}

  getByProduct(productId: number): Observable<ProductRecipe> {
    return this.http.get<ProductRecipe>(`${this.apiUrl}/${productId}`);
  }

  setRecipe(productId: number, items: RecipeItem[]): Observable<any> {
    return this.http.put(`${this.apiUrl}/${productId}`, items);
  }
}
