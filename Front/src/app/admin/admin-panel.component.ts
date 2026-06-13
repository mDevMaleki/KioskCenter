import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProductAdminComponent } from './product-admin/product-admin.component';
import { CategoryAdminComponent } from './category-admin/category-admin.component';
import { ReportsComponent } from './reports/reports.component';
import { PrinterAdminComponent } from './printer-admin/printer-admin.component';
import { PosDeviceAdminComponent } from './pos-device-admin/pos-device-admin.component';
import { PaymentMethodAdminComponent } from './payment-method-admin/payment-method-admin.component';
import { StyleAdminComponent } from './style-admin/style-admin.component';
import { OrderTypeConfigComponent } from './order-type-config/order-type-config.component';
import { Router } from '@angular/router';
import { KioskService } from './../services/kiosk.service';

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
    ReportsComponent
  ],
  templateUrl: './admin-panel.component.html',
  styleUrls: ['./admin-panel.component.css']
})
export class AdminPanelComponent {
  constructor(
    private kioskService: KioskService,
    private router: Router,
  ) {}
  
  activeTab: string = 'order-type';

  setTab(tab: string) {
    this.activeTab = tab;
  }
  
  goBack() {
    this.kioskService.goBack();
    this.router.navigate(['/']);
  }
}