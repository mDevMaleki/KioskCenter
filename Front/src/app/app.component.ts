import { Component, OnInit, Renderer2, Inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule, DOCUMENT } from '@angular/common';
import { IdleService } from './services/idle.service';
import { StyleService, RestaurantStyle } from './services/style.service';
import { LicenseService } from './services/license.service';
import { LicenseActivationComponent } from './license-activation/license-activation.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, LicenseActivationComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  title = 'aghadon-kiosk';
  private currentStyle: RestaurantStyle | null = null;

  licenseChecked = false;
  licensed = false;

  constructor(
    private idle: IdleService,
    private styleService: StyleService,
    private licenseService: LicenseService,
    private renderer: Renderer2,
    @Inject(DOCUMENT) private document: Document
  ) {}

  ngOnInit() {
    this.licenseService.getStatus().subscribe({
      next: (res) => {
        this.licensed = res.licensed;
        this.licenseChecked = true;
      },
      error: () => {
        this.licensed = false;
        this.licenseChecked = true;
      }
    });

    // بارگذاری استایل از سرور
    this.styleService.style$.subscribe(style => {
      if (style) {
        this.currentStyle = style;
        this.applyStyle(style);
      }
    });
  }

  private applyStyle(style: RestaurantStyle) {
  const root = this.document.documentElement;
  
  this.renderer.setStyle(root, '--primary-color', style.primaryColor);
  this.renderer.setStyle(root, '--secondary-color', style.secondaryColor);
  this.renderer.setStyle(root, '--text-color', style.textColor);
  this.renderer.setStyle(root, '--button-color', style.buttonColor);
  this.renderer.setStyle(root, '--button-hover-color', style.buttonHoverColor);
  
  if (style.backgroundColor) {
    this.renderer.setStyle(this.document.body, 'background', style.backgroundColor);
  }
  
  // اعمال فونت سفارشی
  if (style.customFontUrl && style.customFontUrl.trim() !== '') {
    // حذف فونت قبلی
    const oldFont = this.document.getElementById('dynamic-font-face');
    if (oldFont) oldFont.remove();
    
    const fontUrl = style.customFontUrl.startsWith('http') 
      ? style.customFontUrl 
      : `https://localhost:7000${style.customFontUrl}`;
    
    const fontFace = `
      @font-face {
        font-family: '${style.fontName}';
        src: url('${fontUrl}') format('truetype');
        font-weight: ${style.fontWeight};
        font-style: ${style.fontStyle};
      }
    `;
    
    const styleElement = this.renderer.createElement('style');
    this.renderer.setAttribute(styleElement, 'id', 'dynamic-font-face');
    this.renderer.setProperty(styleElement, 'textContent', fontFace);
    this.renderer.appendChild(this.document.head, styleElement);
    
    this.renderer.setStyle(this.document.body, 'font-family', `'${style.fontName}', ${style.fontFamily}`);
  } else {
    this.renderer.setStyle(this.document.body, 'font-family', style.fontFamily);
  }
  
  this.updateMetaTags(style);
}

  private updateMetaTags(style: RestaurantStyle) {
    // به‌روزرسانی title صفحه
    this.renderer.setProperty(this.document, 'title', style.restaurantName);
    
    // به‌روزرسانی meta description (اختیاری)
    let metaDescription = this.document.querySelector('meta[name="description"]');
    if (!metaDescription) {
      metaDescription = this.renderer.createElement('meta');
      this.renderer.setAttribute(metaDescription, 'name', 'description');
      this.renderer.appendChild(this.document.head, metaDescription);
    }
    this.renderer.setAttribute(metaDescription, 'content', `سامانه سفارش آنلاین ${style.restaurantName}`);
  }
}