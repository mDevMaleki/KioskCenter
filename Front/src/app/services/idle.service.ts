import { Injectable, NgZone } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { KioskService } from './kiosk.service';

@Injectable({ providedIn: 'root' })
export class IdleService {
  private timeout: any;
  private idleTime = 60000; // 60 ثانیه
  private currentUrl = '';

  constructor(private kioskService: KioskService, private router: Router, private ngZone: NgZone) {
    this.currentUrl = this.router.url;

    this.router.events.subscribe(event => {
      if (event instanceof NavigationEnd) {
        this.currentUrl = event.urlAfterRedirects;
        this.resetTimer();
      }
    });

    this.resetTimer();
    this.watchUserActivity();
  }

  // بازگشت خودکار به صفحه اول فقط در حالت کیوسک معنا دارد؛ در پنل ادمین و صندوق فروشگاهی هیچ‌گاه نباید اجرا شود
  private get isAdminRoute(): boolean {
    return this.currentUrl.startsWith('/admin') || this.currentUrl.startsWith('/cashier');
  }

  watchUserActivity() {
    window.addEventListener('mousemove', () => this.resetTimer());
    window.addEventListener('click', () => this.resetTimer());
    window.addEventListener('touchstart', () => this.resetTimer());
    window.addEventListener('keydown', () => this.resetTimer());
    window.addEventListener('scroll', () => this.resetTimer());
  }

  resetTimer() {
    clearTimeout(this.timeout);

    if (this.isAdminRoute) {
      return;
    }

    this.ngZone.runOutsideAngular(() => {
      this.timeout = setTimeout(() => {
        if (this.isAdminRoute) return;

        this.ngZone.run(() => {
          this.kioskService.clearCart();
          this.router.navigate(['/']); // صفحه اول
        });
      }, this.idleTime);
    });
  }
}
