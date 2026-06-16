import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProductAdminComponent } from './product-admin/product-admin.component';
import { CategoryAdminComponent } from './category-admin/category-admin.component';
import { ReportsComponent } from './reports/reports.component';
import { PrinterAdminComponent } from './printer-admin/printer-admin.component';
import { PosDeviceAdminComponent } from './pos-device-admin/pos-device-admin.component';
import { PaymentMethodAdminComponent } from './payment-method-admin/payment-method-admin.component';
import { StyleAdminComponent } from './style-admin/style-admin.component';
import { OrderTypeConfigComponent } from './order-type-config/order-type-config.component';
import { InventoryAdminComponent } from './inventory-admin/inventory-admin.component';
import { UserAdminComponent } from './user-admin/user-admin.component';
import { PurchaseSaleAdminComponent } from './purchase-sale-admin/purchase-sale-admin.component';
import { AccountingAdminComponent } from './accounting-admin/accounting-admin.component';
import { LoginComponent } from './login/login.component';
import { Router } from '@angular/router';
import { KioskService } from './../services/kiosk.service';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-admin-panel',
  standalone: true,
  imports: [
    CommonModule,
    ProductAdminComponent,
    CategoryAdminComponent,
    PrinterAdminComponent,
    PosDeviceAdminComponent,
    PaymentMethodAdminComponent,
    StyleAdminComponent,
    OrderTypeConfigComponent,
    InventoryAdminComponent,
    UserAdminComponent,
    PurchaseSaleAdminComponent,
    AccountingAdminComponent,
    ReportsComponent,
    LoginComponent
  ],
  templateUrl: './admin-panel.component.html',
  styleUrls: ['./admin-panel.component.css']
})
export class AdminPanelComponent implements OnInit {
  constructor(
    private kioskService: KioskService,
    private router: Router,
    public authService: AuthService,
  ) {}

  activeTab: string = 'order-type';

  ngOnInit(): void {
    if (this.authService.isLoggedIn()) {
      this.onLoggedIn();
    }
  }

  get tabs() {
    return [
      { key: 'products', label: 'مدیریت محصولات', icon: '🍔' },
      { key: 'categories', label: 'مدیریت دسته‌بندی', icon: '🗂️' },
      { key: 'order-type', label: 'مدیریت انواع سفارش', icon: '📋' },
      { key: 'inventory', label: 'مدیریت انبار', icon: '📦' },
      { key: 'printers', label: 'مدیریت پرینترها', icon: '🖨️' },
      { key: 'pos-devices', label: 'مدیریت دستگاه‌های POS', icon: '💠' },
      { key: 'payment-methods', label: 'مدیریت روش‌های پرداخت', icon: '💳' },
      { key: 'style', label: 'مدیریت استایل', icon: '🎨' },
      { key: 'reports', label: 'گزارشات', icon: '📊' },
      { key: 'users', label: 'مدیریت کاربران', icon: '👤' },
      { key: 'purchase-sale', label: 'خرید و فروش', icon: '🛒' },
      { key: 'accounting', label: 'حسابداری', icon: '📒' },
    ].filter(tab => this.authService.hasPermission(tab.key));
  }

  onLoggedIn(): void {
    const allowedTabs = this.tabs;
    if (!allowedTabs.some(t => t.key === this.activeTab)) {
      const firstTab = allowedTabs[0];
      if (firstTab) {
        this.activeTab = firstTab.key;
      }
    }
  }

  setTab(tab: string) {
    this.activeTab = tab;
  }

  logout(): void {
    this.authService.logout();
  }

  goBack() {
    this.kioskService.goBack();
    this.router.navigate(['/']);
  }
}
