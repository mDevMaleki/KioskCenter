import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { KioskService } from '../../services/kiosk.service';
import { OrderTypeSettingsService } from '../../services/order-type-settings.service';

@Component({
  selector: 'app-order-type',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './order-type.component.html',
  styleUrls: ['./order-type.component.css']
})
export class OrderTypeComponent implements OnInit {
  activeOrderTypes: any[] = [];

  constructor(
    private kioskService: KioskService,
    private orderTypeSettings: OrderTypeSettingsService,
    private router: Router
  ) { }

  ngOnInit() {
    this.activeOrderTypes = this.orderTypeSettings.getActiveOrderTypes();
  }

  selectOrderType(type: string): void {
    this.kioskService.setOrderType(type);
    this.router.navigate(['/products']);
  }

  backToMenu(): void {
    this.kioskService.goBack();
    this.router.navigate(['/']);
  }

  goToAdmin() {
    this.router.navigate(['/admin']);
  }
}