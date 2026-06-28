import { Injectable } from '@angular/core';

interface OrderTypeSetting {
  id: 'EatIn' | 'TakeAway';
  name: string;
  persianName: string;
  icon: string;
  imageUrl?: string;
  active: boolean;
  priceFactor: number;
  visibleCategoryIds: string[];
  categoryPriceRules: {
    categoryId: string;
    factor: number;
  }[];
}

@Injectable({
  providedIn: 'root'
})
export class OrderTypeSettingsService {
  private storageKey = 'order_type_settings';

  getOrderTypeSettings(): OrderTypeSetting[] {
    const defaultSettings: OrderTypeSetting[] = [
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

    const saved = localStorage.getItem(this.storageKey);
    if (saved) {
      return JSON.parse(saved);
    }
    return defaultSettings;
  }

  saveOrderTypeSettings(settings: OrderTypeSetting[]): void {
    localStorage.setItem(this.storageKey, JSON.stringify(settings));
  }

  getActiveOrderTypes(): OrderTypeSetting[] {
    return this.getOrderTypeSettings().filter(t => t.active);
  }

  getSettingsForOrderType(typeId: 'EatIn' | 'TakeAway'): OrderTypeSetting | undefined {
    return this.getOrderTypeSettings().find(t => t.id === typeId);
  }

  calculatePrice(originalPrice: number, orderTypeId: 'EatIn' | 'TakeAway', categoryId?: string): number {
    const settings = this.getSettingsForOrderType(orderTypeId);
    if (!settings) return originalPrice;

    let factor = settings.priceFactor;
    
    // اگر قانون اختصاصی برای این دسته‌بندی وجود دارد
    if (categoryId) {
      const categoryRule = settings.categoryPriceRules.find(r => r.categoryId === categoryId);
      if (categoryRule) {
        factor = categoryRule.factor;
      }
    }
    
    return Math.round(originalPrice * factor * 100) / 100;
  }

  getVisibleCategories(orderTypeId: 'EatIn' | 'TakeAway'): string[] {
    const settings = this.getSettingsForOrderType(orderTypeId);
    return settings?.visibleCategoryIds || [];
  }
}