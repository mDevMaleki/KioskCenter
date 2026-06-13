import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject, of } from 'rxjs';

export interface RestaurantStyle {
  id: number;
  restaurantName: string;
  logoUrl: string;
  
  primaryColor: string;
  secondaryColor: string;
  accentColor: string;
  backgroundColor: string;
  textColor: string;
  textSecondaryColor: string;
  
  buttonColor: string;
  buttonHoverColor: string;
  buttonTextColor: string;
  
  productCardBgColor: string;
  productCardHoverColor: string;
  productCardBorderRadius: string;
  
  sidebarBgColor: string;
  sidebarHeaderBgColor: string;
  sidebarItemBgColor: string;
  sidebarItemHoverColor: string;
  
  categoryBtnBgColor: string;
  categoryBtnActiveBgColor: string;
  categoryBtnTextColor: string;
  categoryBtnActiveTextColor: string;
  
  orderTypeCardBgColor: string;
  orderTypeCardHoverColor: string;
  orderTypeCardTextColor: string;
  
  fontFamily: string;
  customFontUrl: string;
  fontName: string;
  fontWeight: string;
  fontStyle: string;
  fontSizeBase: string;
  fontSizeTitle: string;
  
  backgroundImage: string;
  backgroundOverlay: string;
  
  footerText: string;
  footerBgColor: string;
  footerTextColor: string;
  address: string;
  phone: string;
  instagram: string;
  workingHours: string;
  
  cardBorderRadius: string;
  buttonBorderRadius: string;
  spacingBase: string;
  
  enableAnimations: boolean;
  animationDuration: string;
  
  isActive: boolean;
  updatedAt: Date;
}

@Injectable({
  providedIn: 'root'
})
export class StyleService {
  private styleSubject = new BehaviorSubject<RestaurantStyle | null>(null);
  style$ = this.styleSubject.asObservable();

  constructor() {
    this.loadStyleFromStorage();
  }

  loadStyle(): void {
    this.loadStyleFromStorage();
  }

  private loadStyleFromStorage() {
    const savedStyle = this.loadFromLocalStorage();
    if (savedStyle) {
      this.styleSubject.next(savedStyle);
      this.applyAllStyles(savedStyle);
      console.log('✅ استایل از localStorage لود شد');
    } else {
      const defaultStyle = this.getDefaultStyle();
      this.styleSubject.next(defaultStyle);
      this.applyAllStyles(defaultStyle);
      this.saveToLocalStorage(defaultStyle);
      console.log('✅ استایل پیش‌فرض اعمال شد');
    }
  }

  getStyle(): Observable<RestaurantStyle> {
    const current = this.styleSubject.value;
    if (current) {
      return of(current);
    }
    return of(this.getDefaultStyle());
  }

  updateStyle(style: RestaurantStyle): Observable<RestaurantStyle> {
    // ذخیره در localStorage
    this.saveToLocalStorage(style);
    this.styleSubject.next(style);
    this.applyAllStyles(style);
    
    console.log('✅ استایل ذخیره شد:', style);
    return of(style);
  }

  // متدهای آپلود (برای استفاده بعدی)
  uploadLogo(file: File): Observable<{ url: string }> {
    const fakeUrl = URL.createObjectURL(file);
    return of({ url: fakeUrl });
  }

  uploadBackground(file: File): Observable<{ url: string }> {
    const fakeUrl = URL.createObjectURL(file);
    return of({ url: fakeUrl });
  }

  uploadFont(file: File): Observable<{ url: string }> {
    const fakeUrl = URL.createObjectURL(file);
    return of({ url: fakeUrl });
  }

  private applyAllStyles(style: RestaurantStyle) {
    this.applyGlobalStyles(style);
  }

  private applyGlobalStyles(style: RestaurantStyle) {
    // حذف استایل قبلی
    const existingStyle = document.getElementById('dynamic-style');
    if (existingStyle) existingStyle.remove();
    
    const existingFont = document.getElementById('dynamic-font-face');
    if (existingFont) existingFont.remove();
    
    const existingFooter = document.getElementById('global-footer');
    if (existingFooter) existingFooter.remove();

    // اعمال فونت
    if (style.customFontUrl && style.customFontUrl.trim() !== '') {
      let fontUrl = style.customFontUrl;
      if (!fontUrl.startsWith('http') && !fontUrl.startsWith('blob:')) {
        fontUrl = `http://localhost:5000${fontUrl}`;
      }
      
      const fontFace = document.createElement('style');
      fontFace.id = 'dynamic-font-face';
      fontFace.textContent = `
        @font-face {
          font-family: '${style.fontName}';
          src: url('${fontUrl}') format('truetype');
          font-weight: ${style.fontWeight};
          font-style: ${style.fontStyle};
          font-display: swap;
        }
      `;
      document.head.appendChild(fontFace);
    }

    // اعمال استایل اصلی
    const styleElement = document.createElement('style');
    styleElement.id = 'dynamic-style';
    styleElement.textContent = `
      :root {
        --primary-color: ${style.primaryColor};
        --secondary-color: ${style.secondaryColor};
        --accent-color: ${style.accentColor};
        --background-color: ${style.backgroundColor};
        --text-color: ${style.textColor};
        --text-secondary-color: ${style.textSecondaryColor};
        --button-color: ${style.buttonColor};
        --button-hover-color: ${style.buttonHoverColor};
        --button-text-color: ${style.buttonTextColor};
        --product-card-bg: ${style.productCardBgColor};
        --product-card-hover: ${style.productCardHoverColor};
        --sidebar-bg: ${style.sidebarBgColor};
        --sidebar-header-bg: ${style.sidebarHeaderBgColor};
        --sidebar-item-bg: ${style.sidebarItemBgColor};
        --sidebar-item-hover: ${style.sidebarItemHoverColor};
        --category-btn-bg: ${style.categoryBtnBgColor};
        --category-btn-active-bg: ${style.categoryBtnActiveBgColor};
        --category-btn-text: ${style.categoryBtnTextColor};
        --category-btn-active-text: ${style.categoryBtnActiveTextColor};
        --order-type-card-bg: ${style.orderTypeCardBgColor};
        --order-type-card-hover: ${style.orderTypeCardHoverColor};
        --order-type-card-text: ${style.orderTypeCardTextColor};
        --card-radius: ${style.cardBorderRadius};
        --button-radius: ${style.buttonBorderRadius};
        --font-size-base: ${style.fontSizeBase};
        --animation-duration: ${style.animationDuration};
        --font-family: ${this.getFontFamilyValue(style)};
      }
      
      body {
        font-family: var(--font-family);
        font-weight: ${style.fontWeight};
        font-style: ${style.fontStyle};
        font-size: var(--font-size-base);
        background: var(--background-color);
        color: var(--text-color);
        direction: rtl;
        margin: 0;
        padding: 0;
        min-height: 100vh;
      }
      
      ${style.backgroundImage ? `
      body::before {
        content: '';
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background-image: url('${style.backgroundImage}');
        background-size: cover;
        background-position: center;
        background-repeat: no-repeat;
        opacity: 0.3;
        z-index: -1;
        pointer-events: none;
      }
      ` : ''}
      
      button, .btn, .btn-payment, .btn-save, .btn-action, 
      .btn-back-to-menu, .btn-add, .btn-remove, .btn-upload, 
      .btn-reset, .category-btn, .tab-btn, .pay-btn {
        background-color: var(--button-color) !important;
        color: var(--button-text-color) !important;
        border-radius: var(--button-radius) !important;
        transition: all var(--animation-duration) ease !important;
        border: none;
        cursor: pointer;
      }
      
      button:hover, .btn:hover, .btn-payment:hover, .btn-save:hover, 
      .btn-action:hover, .btn-back-to-menu:hover, .btn-add:hover, 
      .btn-remove:hover, .btn-upload:hover, .btn-reset:hover,
      .category-btn:hover, .tab-btn:hover, .pay-btn:hover {
        background-color: var(--button-hover-color) !important;
        transform: translateY(-2px);
      }
      
      button:disabled, .btn:disabled {
        opacity: 0.5;
        cursor: not-allowed;
        transform: none;
      }
      
      input[type="radio"], 
      input[type="checkbox"] {
        accent-color: var(--primary-color);
        width: 18px;
        height: 18px;
        cursor: pointer;
        transition: all var(--animation-duration) ease;
      }
      
      input[type="radio"]:checked, 
      input[type="checkbox"]:checked {
        accent-color: var(--button-color);
      }
      
      input[type="radio"]:hover, 
      input[type="checkbox"]:hover {
        transform: scale(1.1);
      }
      
      .product-card, .card {
        background: var(--product-card-bg) !important;
        border-radius: var(--card-radius) !important;
        transition: all var(--animation-duration) ease !important;
        cursor: pointer;
        overflow: hidden;
      }
      
      .product-card:hover, .card:hover {
        background: var(--product-card-hover) !important;
        transform: translateY(-6px);
        box-shadow: 0 14px 28px rgba(0, 0, 0, 0.3);
      }
      
      .cart-sidebar {
        background: var(--sidebar-bg) !important;
      }
      
      .cart-sidebar .cart-header {
        background: var(--sidebar-header-bg) !important;
        border-radius: var(--card-radius) var(--card-radius) 0 0 !important;
      }
      
      .cart-item {
        background: var(--sidebar-item-bg) !important;
        border-radius: var(--card-radius) !important;
      }
      
      .cart-item:hover {
        background: var(--sidebar-item-hover) !important;
      }
      
      .cart-sidebar .total-price {
        color: var(--accent-color) !important;
      }
      
      .category-btn {
        background: var(--category-btn-bg) !important;
        color: var(--category-btn-text) !important;
        border-radius: var(--button-radius) !important;
      }
      
      .category-btn.active {
        background: var(--category-btn-active-bg) !important;
        color: var(--category-btn-active-text) !important;
      }
      
      .order-grid .card {
        background: var(--order-type-card-bg) !important;
        border-radius: var(--card-radius) !important;
      }
      
      .order-grid .card:hover {
        background: var(--order-type-card-hover) !important;
      }
      
      .order-grid .card-title {
        color: var(--order-type-card-text) !important;
      }
      
      input, select, textarea {
        background: rgba(255,255,255,0.1);
        color: var(--text-color);
        border: 1px solid var(--secondary-color);
        border-radius: var(--button-radius);
        padding: 10px 12px;
        font-family: inherit;
      }
      
      input:focus, select:focus, textarea:focus {
        outline: none;
        border-color: var(--button-color);
        box-shadow: 0 0 0 3px rgba(40, 167, 69, 0.2);
      }
      
      ::-webkit-scrollbar {
        width: 8px;
        height: 8px;
      }
      
      ::-webkit-scrollbar-track {
        background: var(--secondary-color);
        border-radius: 10px;
      }
      
      ::-webkit-scrollbar-thumb {
        background: var(--primary-color);
        border-radius: 10px;
      }
      
      ${style.enableAnimations ? `
      @keyframes fadeIn {
        from { opacity: 0; transform: translateY(20px); }
        to { opacity: 1; transform: translateY(0); }
      }
      
      .product-card, .cart-item, .category-btn, .card {
        animation: fadeIn 0.5s ease backwards;
      }
      ` : ''}
    `;
    document.head.appendChild(styleElement);

    // فوتر
    if (style.footerText || style.address || style.phone || style.instagram || style.workingHours) {
      const footer = document.createElement('div');
      footer.id = 'global-footer';
      footer.style.cssText = `
        background: ${style.footerBgColor};
        color: ${style.footerTextColor};
        text-align: center;
        padding: 15px;
        font-size: 12px;
        position: fixed;
        bottom: 0;
        left: 0;
        right: 0;
        z-index: 1000;
        direction: rtl;
      `;
      let html = '';
      if (style.footerText) html += `<p>${style.footerText}</p>`;
      if (style.address) html += `<p>📍 ${style.address}</p>`;
      if (style.phone) html += `<p>📞 ${style.phone}</p>`;
      if (style.workingHours) html += `<p>🕒 ${style.workingHours}</p>`;
      if (style.instagram) html += `<p>📷 اینستاگرام: ${style.instagram}</p>`;
      footer.innerHTML = html;
      document.body.appendChild(footer);
      document.body.style.paddingBottom = '80px';
    }

    if (style.restaurantName) {
      document.title = style.restaurantName;
    }
  }

  private getFontFamilyValue(style: RestaurantStyle): string {
    if (style.customFontUrl && style.fontName) {
      return `'${style.fontName}', ${style.fontFamily}`;
    }
    return style.fontFamily;
  }

  private saveToLocalStorage(style: RestaurantStyle) {
    localStorage.setItem('restaurant_style', JSON.stringify(style));
  }

  private loadFromLocalStorage(): RestaurantStyle | null {
    const data = localStorage.getItem('restaurant_style');
    if (data) {
      try {
        return JSON.parse(data);
      } catch {
        return null;
      }
    }
    return null;
  }

  getDefaultStyle(): RestaurantStyle {
    return {
      id: 0,
      restaurantName: 'کافی شاپ آغادون',
      logoUrl: 'assets/images/logo.png',
      primaryColor: '#5C2E15',
      secondaryColor: '#d4b896',
      accentColor: '#ff6b35',
      backgroundColor: 'linear-gradient(135deg, #d4b896 0%, #c9a882 100%)',
      textColor: '#ffffff',
      textSecondaryColor: 'rgba(255,255,255,0.8)',
      buttonColor: '#28a745',
      buttonHoverColor: '#1e7e34',
      buttonTextColor: '#ffffff',
      productCardBgColor: '#5C2E15',
      productCardHoverColor: '#6B3D1F',
      productCardBorderRadius: '24px',
      sidebarBgColor: 'linear-gradient(180deg, #060468 0%, #2326b6 100%)',
      sidebarHeaderBgColor: 'rgba(0,0,0,0.2)',
      sidebarItemBgColor: 'rgba(255,255,255,0.1)',
      sidebarItemHoverColor: 'rgba(255,255,255,0.2)',
      categoryBtnBgColor: 'white',
      categoryBtnActiveBgColor: '#6b4423',
      categoryBtnTextColor: '#6b4423',
      categoryBtnActiveTextColor: 'white',
      orderTypeCardBgColor: '#0084fb',
      orderTypeCardHoverColor: '#0094ff',
      orderTypeCardTextColor: 'white',
      fontFamily: 'YekanBakh, Tahoma, sans-serif',
      customFontUrl: '',
      fontName: 'YekanBakh',
      fontWeight: '500',
      fontStyle: 'normal',
      fontSizeBase: '14px',
      fontSizeTitle: '24px',
      backgroundImage: '',
      backgroundOverlay: 'rgba(0,0,0,0.3)',
      footerText: '',
      footerBgColor: 'rgba(0,0,0,0.8)',
      footerTextColor: '#ffffff',
      address: '',
      phone: '',
      instagram: '',
      workingHours: '',
      cardBorderRadius: '20px',
      buttonBorderRadius: '8px',
      spacingBase: '16px',
      enableAnimations: true,
      animationDuration: '0.3s',
      isActive: true,
      updatedAt: new Date()
    };
  }

  getCurrentStyle(): RestaurantStyle | null {
    return this.styleSubject.value;
  }
}