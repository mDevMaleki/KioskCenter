import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { KioskService } from '../../services/kiosk.service';

@Component({
  selector: 'app-product-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './product-admin.component.html',
  styleUrls: ['./product-admin.component.css']
})
export class ProductAdminComponent implements OnInit {

  products: any[] = [];
  categories: any[] = [];

  // فرم مشترک برای افزودن/ویرایش
  newProduct: any = {
    id: null,
    name: '',
    price: 0,           // قیمت اول
    secondPrice: null,  // قیمت دوم
    categoryId: '',
    imageUrl: '',
    description: '',
    state: 1
  };

  editingProductId: number | null = null;

  constructor(private kioskService: KioskService) {}

  ngOnInit(): void {
    this.loadProducts();
    this.loadCategories();
  }

  loadProducts(): void {
    this.kioskService.getProducts().subscribe({
      next: (res: any) => {
        this.products = res;
      },
      error: err => console.error('خطا در دریافت محصولات', err)
    });
  }

  loadCategories(): void {
    this.kioskService.getCategories().subscribe({
      next: (res: any) => {
        this.categories = res;
      },
      error: err => console.error('خطا در دریافت دسته‌بندی‌ها', err)
    });
  }

  onFileSelected(event: any): void {
    const file: File = event.target.files[0];
    if (!file) { return; }

    this.kioskService.uploadImage(file).subscribe({
      next: (res: any) => {
        this.newProduct.imageUrl = res.imageUrl;
      },
      error: err => console.error('خطا در آپلود تصویر', err)
    });
  }

  saveProduct(): void {
    // منطق قیمت‌ها:
    // اگر قیمت دوم وارد نشده (null یا 0 یا خالی)، آن را برابر قیمت اول قرار بده
    let finalSecondPrice = this.newProduct.secondPrice;
    
    if (!finalSecondPrice || finalSecondPrice === 0 || finalSecondPrice === '') {
      finalSecondPrice = this.newProduct.price;
    }

    const productPayload = {
      id: this.newProduct.id,
      name: this.newProduct.name,
      price: Number(this.newProduct.price),           // قیمت اول
      secondPrice: Number(finalSecondPrice),          // قیمت دوم (اگر وارد نشده بود = قیمت اول)
      categoryId: Number(this.newProduct.categoryId),
      imageUrl: this.newProduct.imageUrl,
      description: this.newProduct.description,
      state: Number(this.newProduct.state)
    };

    if (this.editingProductId) {
      this.kioskService.updateProduct(productPayload).subscribe({
        next: () => {
          this.loadProducts();
          this.resetForm();
        },
        error: err => console.error('خطا در ویرایش محصول', err)
      });
    } else {
      this.kioskService.addProduct(productPayload).subscribe({
        next: () => {
          this.loadProducts();
          this.resetForm();
        },
        error: err => console.error('خطا در افزودن محصول', err)
      });
    }
  }

  editProduct(product: any): void {
    this.editingProductId = product.id;
    this.newProduct = {
      id: product.id,
      name: product.name,
      price: product.price,           // قیمت اول
      secondPrice: product.secondPrice, // قیمت دوم
      categoryId: product.categoryId,
      imageUrl: product.imageUrl,
      description: product.description,
      state: product.state
    };
  }

  deleteProduct(id: number): void {
    if (!confirm('آیا از حذف این محصول مطمئن هستید؟')) {
      return;
    }

    this.kioskService.deleteProduct(id).subscribe({
      next: () => {
        this.loadProducts();
        if (this.editingProductId === id) {
          this.resetForm();
        }
      },
      error: err => console.error('خطا در حذف محصول', err)
    });
  }

  resetForm(): void {
    this.newProduct = {
      id: null,
      name: '',
      price: 0,
      secondPrice: null,
      categoryId: '',
      imageUrl: '',
      description: '',
      state: 1
    };
    this.editingProductId = null;
  }
}