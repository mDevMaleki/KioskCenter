import { Injectable, NgZone } from '@angular/core';
import { Router } from '@angular/router';
import {KioskService} from './kiosk.service';
@Injectable({ providedIn: 'root' })
export class IdleService {
  private timeout: any;
  private idleTime = 60000; // 15 seconds

  constructor(private kioskService:KioskService, private router: Router, private ngZone: NgZone) {
    this.resetTimer();
    this.watchUserActivity();
  }

  watchUserActivity() {
    window.addEventListener('mousemove', () => this.resetTimer());
    window.addEventListener('click', () => this.resetTimer());
    window.addEventListener('touchstart', () => this.resetTimer());
  }

  resetTimer() {
    clearTimeout(this.timeout);

    this.ngZone.runOutsideAngular(() => {
      this.timeout = setTimeout(() => {
        this.ngZone.run(() => {
            this.kioskService.clearCart();
          this.router.navigate(['/']); // صفحه اول
        });
      }, this.idleTime);
    });
  }
}
