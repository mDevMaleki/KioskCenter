import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { CartItem, Product, Category } from '../../models/kiosk.models';
import { KioskService } from '../../services/kiosk.service';
import { StyleService, RestaurantStyle } from '../../services/style.service';

interface OrderTypeSetting {
  id: string;
  persianName: string;
  icon: string;
  active: boolean;
  priceFactor: number;
  visibleCategoryIds: string[];
  categoryPriceRules: { categoryId: string; factor: number }[];
}

@Component({
  selector: 'app-product-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './product-select.component.html',
  styleUrls: ['./product-select.component.css']
})
export class ProductSelectComponent implements OnInit, OnDestroy {
  products: Product[] = [];
  cart: CartItem[] = [];
  loading = false;
  categories: Category[] = [];
  filteredCategories: Category[] = [];
  selectedCategoryId: number | null = null;
  orderType: string = '';
  orderTypeSettings: OrderTypeSetting | null = null;
  currentStyle: RestaurantStyle | null = null;
  private styleSubscription: Subscription | null = null;

  constructor(
    private kioskService: KioskService,
    private styleService: StyleService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadOrderTypeSettings();
    this.loadCategories();
    
    this.kioskService.state$.subscribe(state => {
      this.cart = state.cart;
      this.orderType = state.orderType;
      if (this.orderType) {
        this.loadOrderTypeSettings();
      }
    });

    // اشتراک برای دریافت استایل
    this.styleSubscription = this.styleService.style$.subscribe(style => {
      if (style) {
        this.currentStyle = style;
      }
    });
  }

  ngOnDestroy() {
    if (this.styleSubscription) {
      this.styleSubscription.unsubscribe();
    }
  }

  loadOrderTypeSettings(): void {
    const saved = localStorage.getItem('order_type_settings');
    if (saved) {
      const settings: OrderTypeSetting[] = JSON.parse(saved);
      this.orderTypeSettings = settings.find(s => s.id === this.orderType) || null;
      this.filterCategoriesBySettings();
      this.loadProducts(this.selectedCategoryId);
    } else {
      this.orderTypeSettings = {
        id: this.orderType,
        persianName: this.orderType === 'EatIn' ? 'داخل سالن' : 'بیرون بر',
        icon: this.orderType === 'EatIn' ? '🏠' : '🛍️',
        active: true,
        priceFactor: this.orderType === 'EatIn' ? 1 : 1.1,
        visibleCategoryIds: [],
        categoryPriceRules: []
      };
      this.loadProducts(this.selectedCategoryId);
    }
  }

  filterCategoriesBySettings(): void {
    if (!this.orderTypeSettings || !this.orderTypeSettings.visibleCategoryIds.length) {
      this.filteredCategories = [...this.categories];
      return;
    }

    this.filteredCategories = this.categories.filter(cat => 
      this.orderTypeSettings!.visibleCategoryIds.includes(cat.id.toString())
    );
  }

  calculateFinalPrice(product: Product): number {
    if (!this.orderTypeSettings || !this.orderTypeSettings.active) {
      return product.price;
    }

    let factor = this.orderTypeSettings.priceFactor;

    const categoryRule = this.orderTypeSettings.categoryPriceRules.find(
      rule => rule.categoryId === product.categoryId.toString()
    );
    
    if (categoryRule) {
      factor = categoryRule.factor;
    }

    return Math.round(product.price * factor);
  }

  getProductPrice(product: Product): number {
    return this.calculateFinalPrice(product);
  }

  loadProducts(categoryId: number | null = null): void {
    this.loading = true;
    
    const request = categoryId === null 
      ? this.kioskService.getAllProducts()
      : this.kioskService.getProductsByCategory(categoryId);
    
    request.subscribe({
      next: (data) => {
        let products = data.filter((p: Product) => p.state === 1);
        
        if (this.orderTypeSettings && this.orderTypeSettings.visibleCategoryIds.length > 0) {
          products = products.filter(p => 
            this.orderTypeSettings!.visibleCategoryIds.includes(p.categoryId.toString())
          );
        }
        
        this.products = products;
        this.loading = false;
      },
      error: (error) => {
        console.error('خطا در بارگذاری محصولات:', error);
        this.loading = false;
      }
    });
  }

  loadCategories(): void {
    this.kioskService.getCategories().subscribe({
      next: (data) => {
        this.categories = data;
        this.filterCategoriesBySettings();
      },
      error: (err) => console.error('خطا در دریافت دسته‌بندی‌ها:', err)
    });
  }

  selectCategory(categoryId: number | null): void {
    this.selectedCategoryId = categoryId;
    this.loadProducts(categoryId);
  }

  addToCart(product: Product): void {
    const productWithCorrectPrice = {
      ...product,
      price: this.getProductPrice(product)
    };
    this.kioskService.addToCart(productWithCorrectPrice);
  }

updateQuantity(productId: number, delta: number): void {
  this.kioskService.updateQuantity(productId, delta);
}

  getTotal(): number {
    return this.cart.reduce((total, item) => total + item.totalPrice, 0);
  }

  formatPrice(price: number): string {
    return price.toLocaleString('fa-IR');
  }

  backToMenu(): void {
    this.router.navigate(['/order-type']);
  }

  goToPayment(): void {
    if (this.cart.length === 0) {
      return;
    }
    this.router.navigate(['/payment']);
  }

  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = 'assets/images/coffee-bar.jpg';
  }

  getPriceDisplayText(product: Product): string {
    const finalPrice = this.getProductPrice(product);
    const originalPrice = product.price;
    
    if (finalPrice !== originalPrice) {
      const percentage = Math.round(((finalPrice - originalPrice) / originalPrice) * 100);
      if (percentage > 0) {
        return `${this.formatPrice(finalPrice)} تومان (${percentage}+% بیرون بر)`;
      } else if (percentage < 0) {
        return `${this.formatPrice(finalPrice)} تومان (${Math.abs(percentage)}% تخفیف)`;
      }
    }
    return `${this.formatPrice(finalPrice)} تومان`;
  }

  hasPriceChange(product: Product): boolean {
    return this.getProductPrice(product) !== product.price;
  }

  getOriginalPriceForDisplay(product: Product): number | null {
    const finalPrice = this.getProductPrice(product);
    if (finalPrice !== product.price) {
      return product.price;
    }
    return null;
  }

  // متد برای دریافت استایل داینامیک
  getPrimaryColor(): string {
    return this.currentStyle?.primaryColor || '#5C2E15';
  }

  getTextColor(): string {
    return this.currentStyle?.textColor || '#ffffff';
  }

  getLogoUrl(): string {
    const url = this.currentStyle?.logoUrl;
    return url ? this.styleService.resolveAssetUrl(url) : 'assets/logo.png';
  }

  onLogoError(event: Event): void {
    (event.target as HTMLImageElement).src = 'assets/logo.png';
  }
}