import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { KioskService } from '../../services/kiosk.service';

interface OrderTypeSetting {
  id: 'EatIn' | 'TakeAway';
  name: string;
  persianName: string;
  icon: string;
  active: boolean;
  priceFactor: number;
  visibleCategoryIds: string[];
  categoryPriceRules: {
    categoryId: string;
    factor: number;
  }[];
}

interface Category {
  id: number;
  name: string;
  persianName?: string;
  description?: string;
}

@Component({
  selector: 'app-order-type-config',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './order-type-config.component.html',
  styleUrls: ['./order-type-config.component.css']
})
export class OrderTypeConfigComponent implements OnInit {
  orderTypes: OrderTypeSetting[] = [];
  selectedTypeId: 'EatIn' | 'TakeAway' = 'EatIn';
  allCategories: Category[] = [];
  isLoading = false;
  errorMessage = '';
  
  newRule = {
    categoryId: '',
    factor: 1
  };

  constructor(private kioskService: KioskService) {}

  ngOnInit() {
    this.loadSettings();
    this.loadCategoriesFromApi();
  }

  loadSettings() {
    const saved = localStorage.getItem('order_type_settings');
    if (saved) {
      this.orderTypes = JSON.parse(saved);
    } else {
      // تنظیمات پیش‌فرض
      this.orderTypes = [
        {
          id: 'EatIn',
          name: 'EatIn',
          persianName: 'داخل سالن',
          icon: '🏠',
          active: true,
          priceFactor: 1,
          visibleCategoryIds: [],
          categoryPriceRules: []
        },
        {
          id: 'TakeAway',
          name: 'TakeAway',
          persianName: 'بیرون بر',
          icon: '🛍️',
          active: true,
          priceFactor: 1.1,
          visibleCategoryIds: [],
          categoryPriceRules: []
        }
      ];
    }
  }

  // دریافت دسته‌بندی‌ها از API بک‌اند
  loadCategoriesFromApi() {
    this.isLoading = true;
    this.errorMessage = '';
    
    this.kioskService.getCategories().subscribe({
      next: (categories: Category[]) => {
        this.allCategories = categories;
        this.isLoading = false;
        
        // ذخیره در localStorage برای استفاده بعدی
        localStorage.setItem('categories', JSON.stringify(categories));
      },
      error: (error) => {
        console.error('Error loading categories from API:', error);
        this.errorMessage = 'خطا در دریافت دسته‌بندی‌ها از سرور';
        this.isLoading = false;
        
        // fallback به localStorage
        const savedCategories = localStorage.getItem('categories');
        if (savedCategories) {
          this.allCategories = JSON.parse(savedCategories);
        } else {
          // دسته‌بندی‌های پیش‌فرض
          this.allCategories = [
            { id: 1, name: 'Pizza', persianName: 'پیتزا' },
            { id: 2, name: 'Burger', persianName: 'برگر' },
            { id: 3, name: 'Drinks', persianName: 'نوشیدنی‌ها' },
            { id: 4, name: 'Desserts', persianName: 'دسرها' },
            { id: 5, name: 'Appetizers', persianName: 'پیش‌غذاها' }
          ];
        }
      }
    });
  }

  get currentSettings(): OrderTypeSetting | undefined {
    return this.orderTypes.find(t => t.id === this.selectedTypeId);
  }

  selectType(typeId: 'EatIn' | 'TakeAway') {
    this.selectedTypeId = typeId;
    this.resetNewRule();
  }

  resetNewRule() {
    this.newRule = { categoryId: '', factor: 1 };
  }

  saveSettings() {
    // ذخیره در localStorage
    localStorage.setItem('order_type_settings', JSON.stringify(this.orderTypes));
    
    // اگر سرویس save دارید، به سرور هم ارسال کنید
    if (this.kioskService.saveOrderTypeSettings) {
      this.kioskService.saveOrderTypeSettings(this.orderTypes).subscribe({
        next: (response) => {
          console.log('Settings saved to server:', response);
          alert('✅ تنظیمات با موفقیت ذخیره شد!');
        },
        error: (error) => {
          console.error('Error saving to server:', error);
          alert('✅ تنظیمات به صورت محلی ذخیره شد (ارسال به سرور با خطا مواجه شد)');
        }
      });
    } else {
      alert('✅ تنظیمات با موفقیت ذخیره شد!');
    }
  }

  toggleCategory(categoryId: number, event: any) {
    const isChecked = event.target.checked;
    const settings = this.currentSettings;
    if (!settings) return;

    const categoryIdStr = categoryId.toString();

    if (isChecked) {
      if (!settings.visibleCategoryIds.includes(categoryIdStr)) {
        settings.visibleCategoryIds.push(categoryIdStr);
      }
    } else {
      const index = settings.visibleCategoryIds.indexOf(categoryIdStr);
      if (index > -1) {
        settings.visibleCategoryIds.splice(index, 1);
      }
    }
  }

  isCategoryVisible(categoryId: number): boolean {
    return this.currentSettings?.visibleCategoryIds.includes(categoryId.toString()) || false;
  }

  addPriceRule() {
    if (!this.newRule.categoryId || this.newRule.factor <= 0) {
      alert('لطفاً دسته‌بندی و ضریب معتبر وارد کنید');
      return;
    }
    
    const settings = this.currentSettings;
    if (!settings) return;

    const existing = settings.categoryPriceRules.find(r => r.categoryId === this.newRule.categoryId);
    if (existing) {
      existing.factor = this.newRule.factor;
    } else {
      settings.categoryPriceRules.push({ ...this.newRule });
    }
    
    this.resetNewRule();
    alert('✅ قانون قیمتی اضافه شد');
  }

  removePriceRule(categoryId: string) {
    const settings = this.currentSettings;
    if (settings) {
      settings.categoryPriceRules = settings.categoryPriceRules.filter(r => r.categoryId !== categoryId);
    }
  }

  getCategoryName(categoryId: string): string {
    const cat = this.allCategories.find(c => c.id.toString() === categoryId);
    return cat?.persianName || cat?.name || categoryId;
  }

  getRuleFactor(categoryId: string): number {
    const rule = this.currentSettings?.categoryPriceRules.find(r => r.categoryId === categoryId);
    return rule?.factor || 1;
  }

  getFactorDisplay(factor: number): string {
    if (factor > 1) return `🔺 +${((factor - 1) * 100)}%`;
    if (factor < 1) return `🔻 -${((1 - factor) * 100)}%`;
    return '⚖️ بدون تغییر';
  }

  selectAllCategories() {
    const settings = this.currentSettings;
    if (settings) {
      settings.visibleCategoryIds = this.allCategories.map(c => c.id.toString());
    }
  }

  deselectAllCategories() {
    const settings = this.currentSettings;
    if (settings) {
      settings.visibleCategoryIds = [];
    }
  }

  // اضافه کردن متد برای رفرش کردن دسته‌بندی‌ها
  refreshCategories() {
    this.loadCategoriesFromApi();
  }
}