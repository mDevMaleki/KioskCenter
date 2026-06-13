import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StyleService, RestaurantStyle } from '../../services/style.service';

@Component({
  selector: 'app-style-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './style-admin.component.html',
  styleUrls: ['./style-admin.component.css']
})
export class StyleAdminComponent implements OnInit {
  style: RestaurantStyle | null = null;
  isLoading = false;
  selectedFile: File | null = null;
  selectedFontFile: File | null = null;
  selectedBackgroundFile: File | null = null;
  previewUrl: string | null = null;
  showColorPicker = false;
  activeColorField = '';

  constructor(private styleService: StyleService) {}

  ngOnInit() {
    this.loadStyle();
  }

  loadStyle() {
    this.isLoading = true;
    this.styleService.getStyle().subscribe({
      next: (res) => {
        this.style = res;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading style:', err);
        this.style = this.styleService.getDefaultStyle();
        this.isLoading = false;
      }
    });
  }

  onFileSelected(event: any) {
    this.selectedFile = event.target.files[0];
    if (this.selectedFile) {
      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.previewUrl = e.target.result;
      };
      reader.readAsDataURL(this.selectedFile);
    }
  }

  onFontSelected(event: any) {
    this.selectedFontFile = event.target.files[0];
  }

  onBackgroundSelected(event: any) {
    this.selectedBackgroundFile = event.target.files[0];
  }

  uploadLogo() {
    if (!this.selectedFile) return;
    
    this.styleService.uploadLogo(this.selectedFile).subscribe({
      next: (res) => {
        if (this.style) {
          this.style.logoUrl = res.url;
          this.saveStyle();
        }
        this.selectedFile = null;
        this.previewUrl = null;
        alert('لوگو با موفقیت آپلود شد');
      },
      error: (err) => console.error('Error uploading logo:', err)
    });
  }

  uploadBackground() {
    if (!this.selectedBackgroundFile) return;
    
    this.styleService.uploadBackground(this.selectedBackgroundFile).subscribe({
      next: (res) => {
        if (this.style) {
          this.style.backgroundImage = res.url;
          this.saveStyle();
        }
        this.selectedBackgroundFile = null;
        alert('تصویر بک‌گراند با موفقیت آپلود شد');
      },
      error: (err) => console.error('Error uploading background:', err)
    });
  }

  removeBackground() {
    if (this.style && confirm('آیا از حذف تصویر بک‌گراند اطمینان دارید؟')) {
      this.style.backgroundImage = '';
      this.saveStyle();
    }
  }

  uploadFont() {
    if (!this.selectedFontFile) {
      alert('لطفاً یک فایل فونت انتخاب کنید');
      return;
    }
    
    this.isLoading = true;
    this.styleService.uploadFont(this.selectedFontFile).subscribe({
      next: (res) => {
        console.log('Font uploaded:', res);
        if (this.style && res.url) {
          this.style.customFontUrl = res.url;
          this.saveStyle();
          alert('فونت با موفقیت آپلود شد');
        }
        this.selectedFontFile = null;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error uploading font:', err);
        alert('خطا در آپلود فونت: ' + (err.error?.message || err.message));
        this.isLoading = false;
      }
    });
  }

  removeFont() {
    if (this.style && confirm('آیا از حذف فونت سفارشی اطمینان دارید؟')) {
      this.style.customFontUrl = '';
      this.style.fontName = 'YekanBakh';
      this.saveStyle();
    }
  }

 saveStyle() {
  if (!this.style) return;
  
  this.isLoading = true;
  this.styleService.updateStyle(this.style).subscribe({
    next: () => {
      this.isLoading = false;
      alert('✅ تنظیمات با موفقیت ذخیره شد');
      // رفرش صفحه برای اعمال تغییرات
      setTimeout(() => {
        window.location.reload();
      }, 500);
    },
    error: (err) => {
      console.error('Error saving style:', err);
      this.isLoading = false;
      alert('تنظیمات به صورت محلی ذخیره شد');
    }
  });
}

  openColorPicker(field: string) {
    this.activeColorField = field;
    this.showColorPicker = true;
  }

  selectColor(color: string) {
    if (this.style) {
      switch (this.activeColorField) {
        case 'primaryColor':
          this.style.primaryColor = color;
          break;
        case 'secondaryColor':
          this.style.secondaryColor = color;
          break;
        case 'accentColor':
          this.style.accentColor = color;
          break;
        case 'textColor':
          this.style.textColor = color;
          break;
        case 'textSecondaryColor':
          this.style.textSecondaryColor = color;
          break;
        case 'buttonColor':
          this.style.buttonColor = color;
          break;
        case 'buttonHoverColor':
          this.style.buttonHoverColor = color;
          break;
        case 'buttonTextColor':
          this.style.buttonTextColor = color;
          break;
        case 'productCardBgColor':
          this.style.productCardBgColor = color;
          break;
        case 'productCardHoverColor':
          this.style.productCardHoverColor = color;
          break;
        case 'sidebarBgColor':
          this.style.sidebarBgColor = color;
          break;
        case 'sidebarHeaderBgColor':
          this.style.sidebarHeaderBgColor = color;
          break;
        case 'sidebarItemBgColor':
          this.style.sidebarItemBgColor = color;
          break;
        case 'sidebarItemHoverColor':
          this.style.sidebarItemHoverColor = color;
          break;
        case 'categoryBtnBgColor':
          this.style.categoryBtnBgColor = color;
          break;
        case 'categoryBtnActiveBgColor':
          this.style.categoryBtnActiveBgColor = color;
          break;
        case 'categoryBtnTextColor':
          this.style.categoryBtnTextColor = color;
          break;
        case 'categoryBtnActiveTextColor':
          this.style.categoryBtnActiveTextColor = color;
          break;
        case 'orderTypeCardBgColor':
          this.style.orderTypeCardBgColor = color;
          break;
        case 'orderTypeCardHoverColor':
          this.style.orderTypeCardHoverColor = color;
          break;
        case 'orderTypeCardTextColor':
          this.style.orderTypeCardTextColor = color;
          break;
        case 'footerBgColor':
          this.style.footerBgColor = color;
          break;
        case 'footerTextColor':
          this.style.footerTextColor = color;
          break;
      }
    }
    this.showColorPicker = false;
  }

  onPreviewButtonHover(event: MouseEvent, isHover: boolean) {
    const button = event.target as HTMLButtonElement;
    if (isHover && this.style) {
      button.style.backgroundColor = this.style.buttonHoverColor;
    } else if (this.style) {
      button.style.backgroundColor = this.style.buttonColor;
    }
  }

  getFontFamilyValue(): string {
    if (!this.style) return 'YekanBakh, Tahoma, sans-serif';
    if (this.style.customFontUrl && this.style.fontName) {
      return `'${this.style.fontName}', ${this.style.fontFamily}`;
    }
    return this.style.fontFamily;
  }

  colorOptions = [
    '#5C2E15', '#8B4513', '#A0522D', '#D2691E', '#CD853F', '#d4b896', '#c9a882',
    '#f5deb3', '#deb887', '#d2b48c', '#28a745', '#1e7e34', '#dc3545', '#007bff',
    '#6c757d', '#343a40', '#f8f9fa', '#212529', '#ff6b35', '#17a2b8', '#ffc107',
    '#6610f2', '#e83e8c', '#fd7e14', '#20c997', '#6f42c1'
  ];

  resetToDefault() {
    if (confirm('آیا از بازنشانی تنظیمات به حالت پیش‌فرض اطمینان دارید؟')) {
      this.style = this.styleService.getDefaultStyle();
      this.saveStyle();
    }
  }
}