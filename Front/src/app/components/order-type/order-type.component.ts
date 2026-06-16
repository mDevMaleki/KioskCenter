import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { KioskService } from '../../services/kiosk.service';
import { OrderTypeSettingsService } from '../../services/order-type-settings.service';
import { StyleService, RestaurantStyle } from '../../services/style.service';

@Component({
  selector: 'app-order-type',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './order-type.component.html',
  styleUrls: ['./order-type.component.css']
})
export class OrderTypeComponent implements OnInit, OnDestroy {
  activeOrderTypes: any[] = [];
  private currentStyle: RestaurantStyle | null = null;
  private styleSubscription: Subscription | null = null;

  constructor(
    private kioskService: KioskService,
    private orderTypeSettings: OrderTypeSettingsService,
    private styleService: StyleService,
    private router: Router
  ) { }

  ngOnInit() {
    this.activeOrderTypes = this.orderTypeSettings.getActiveOrderTypes();
    this.styleSubscription = this.styleService.style$.subscribe(style => {
      this.currentStyle = style;
    });
  }

  ngOnDestroy() {
    this.styleSubscription?.unsubscribe();
  }

  getLogoUrl(): string {
    const url = this.currentStyle?.logoUrl;
    return url ? this.styleService.resolveAssetUrl(url) : 'assets/logo.png';
  }

  getOrderTypeImage(url: string): string {
    return this.styleService.resolveAssetUrl(url);
  }

  onLogoError(event: Event): void {
    (event.target as HTMLImageElement).src = 'assets/logo.png';
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