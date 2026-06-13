import { Routes } from '@angular/router';
import { CategorySelectComponent } from './components/category-select/category-select.component';
import { ProductSelectComponent } from './components/product-select/product-select.component';
import { OrderTypeComponent } from './components/order-type/order-type.component';
import { PaymentComponent } from './components/payment/payment.component';
import { HashLocationStrategy, LocationStrategy } from '@angular/common';

// کامپوننت مادر ادمین که تب‌ها داخل آن هستند
import { AdminPanelComponent } from './admin/admin-panel.component'; 

export const routes: Routes = [
  { path: '', component: CategorySelectComponent },
  { path: 'products', component: ProductSelectComponent },
  { path: 'order-type', component: OrderTypeComponent },
  { path: 'payment', component: PaymentComponent },
  
  // مسیر جدید برای پنل ادمین
  { path: 'admin', component: AdminPanelComponent },
  
  { path: '**', redirectTo: '' }
];
