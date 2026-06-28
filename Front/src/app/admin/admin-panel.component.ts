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

interface AdminSubItem {
  key: string;
  label: string;
  icon: string;
}

interface AdminTab {
  key: string;
  label: string;
  icon: string;
  subItems?: AdminSubItem[];
}

interface AdminMenuGroup {
  key: string;
  label: string;
  icon: string;
  tabs: AdminTab[];
}

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
  activeSubSection: string | null = null;
  openGroupKey: string = '';
  openTabKey: string = '';

  ngOnInit(): void {
    if (this.authService.isLoggedIn()) {
      this.onLoggedIn();
    }
  }

  private allGroups: AdminMenuGroup[] = [
    {
      key: 'catalog', label: 'مدیریت محصولات', icon: '🍔',
      tabs: [
        { key: 'products', label: 'مدیریت محصولات', icon: '🍔' },
        { key: 'categories', label: 'مدیریت دسته‌بندی', icon: '🗂️' },
        { key: 'order-type', label: 'مدیریت انواع سفارش', icon: '📋' },
        { key: 'inventory', label: 'مدیریت انبار', icon: '📦' },
      ]
    },
    {
      key: 'commerce', label: 'خرید، فروش و حسابداری', icon: '🛒',
      tabs: [
        {
          key: 'purchase-sale', label: 'خرید و فروش', icon: '🛒',
          subItems: [
            { key: 'parties', label: 'طرف‌های حساب', icon: '👥' },
            { key: 'units', label: 'واحدها', icon: '📐' },
            { key: 'materials', label: 'مواد اولیه', icon: '🧂' },
            { key: 'purchaseInvoices', label: 'فاکتور خرید', icon: '🧾' },
            { key: 'products', label: 'محصولات', icon: '🍱' },
            { key: 'saleInvoices', label: 'فاکتور فروش', icon: '🧾' },
          ]
        },
        {
          key: 'accounting', label: 'حسابداری', icon: '📒',
          subItems: [
            { key: 'accounts', label: 'کدینگ حساب‌ها', icon: '🧾' },
            { key: 'cashAccounts', label: 'صندوق و بانک', icon: '🏦' },
            { key: 'journalEntries', label: 'اسناد حسابداری', icon: '📝' },
            { key: 'journalBook', label: 'دفتر روزنامه', icon: '📔' },
            { key: 'trialBalance', label: 'تراز آزمایشی', icon: '⚖️' },
            { key: 'profitLoss', label: 'سود و زیان', icon: '📈' },
            { key: 'balanceSheet', label: 'ترازنامه', icon: '📋' },
            { key: 'vatReport', label: 'گزارش ارزش افزوده', icon: '🧮' },
            { key: 'cheques', label: 'مدیریت چک', icon: '🧷' },
            { key: 'pettyCash', label: 'تنخواه‌گردان', icon: '👛' },
            { key: 'expenses', label: 'هزینه‌ها', icon: '🧯' },
            { key: 'fixedAssets', label: 'دارایی ثابت', icon: '🏭' },
            { key: 'budget', label: 'بودجه', icon: '🎯' },
            { key: 'fiscalYear', label: 'سال مالی و مالیات', icon: '🗓️' },
            { key: 'moadian', label: 'سامانه مودیان', icon: '🏛️' },
          ]
        },
      ]
    },
    {
      key: 'equipment', label: 'تجهیزات و تنظیمات', icon: '⚙️',
      tabs: [
        { key: 'printers', label: 'مدیریت پرینترها', icon: '🖨️' },
        { key: 'pos-devices', label: 'مدیریت دستگاه‌های POS', icon: '💠' },
        { key: 'payment-methods', label: 'مدیریت روش‌های پرداخت', icon: '💳' },
        { key: 'style', label: 'مدیریت استایل', icon: '🎨' },
      ]
    },
    {
      key: 'reportsUsers', label: 'گزارش و کاربران', icon: '📊',
      tabs: [
        { key: 'reports', label: 'گزارشات', icon: '📊' },
        { key: 'users', label: 'مدیریت کاربران', icon: '👤' },
      ]
    },
  ];

  get menuGroups(): AdminMenuGroup[] {
    return this.allGroups
      .map(group => ({
        ...group,
        tabs: group.tabs.filter(tab => this.authService.hasPermission(tab.key))
      }))
      .filter(group => group.tabs.length > 0);
  }

  get tabs() {
    return this.menuGroups.flatMap(g => g.tabs);
  }

  toggleGroup(groupKey: string): void {
    this.openGroupKey = this.openGroupKey === groupKey ? '' : groupKey;
  }

  isGroupOpen(groupKey: string): boolean {
    return this.openGroupKey === groupKey;
  }

  toggleTab(tab: AdminTab): void {
    if (tab.subItems && tab.subItems.length > 0) {
      this.openTabKey = this.openTabKey === tab.key ? '' : tab.key;
      this.activeTab = tab.key;
      if (!this.activeSubSection || this.openTabKey !== tab.key) {
        this.activeSubSection = tab.subItems[0].key;
      }
    } else {
      this.setTab(tab.key);
    }
  }

  isTabOpen(tabKey: string): boolean {
    return this.openTabKey === tabKey;
  }

  setSubSection(tab: AdminTab, subKey: string): void {
    this.activeTab = tab.key;
    this.openTabKey = tab.key;
    this.activeSubSection = subKey;
  }

  onLoggedIn(): void {
    const allowedTabs = this.tabs;
    if (!allowedTabs.some(t => t.key === this.activeTab)) {
      const firstGroup = this.menuGroups[0];
      if (firstGroup) {
        this.openGroupKey = firstGroup.key;
        const firstTab = firstGroup.tabs[0];
        if (firstTab) {
          this.activeTab = firstTab.key;
          if (firstTab.subItems && firstTab.subItems.length > 0) {
            this.openTabKey = firstTab.key;
            this.activeSubSection = firstTab.subItems[0].key;
          }
        }
      }
    } else {
      const group = this.menuGroups.find(g => g.tabs.some(t => t.key === this.activeTab));
      if (group) this.openGroupKey = group.key;
    }
  }

  setTab(tab: string) {
    this.activeTab = tab;
    this.activeSubSection = null;
    this.openTabKey = '';
    const group = this.menuGroups.find(g => g.tabs.some(t => t.key === tab));
    if (group) this.openGroupKey = group.key;
  }

  logout(): void {
    this.authService.logout();
  }

  goBack() {
    this.kioskService.goBack();
    this.router.navigate(['/']);
  }
}
