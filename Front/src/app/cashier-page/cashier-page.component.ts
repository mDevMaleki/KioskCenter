import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LoginComponent } from '../admin/login/login.component';
import { CashierAdminComponent } from '../admin/cashier-admin/cashier-admin.component';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-cashier-page',
  standalone: true,
  imports: [CommonModule, LoginComponent, CashierAdminComponent],
  templateUrl: './cashier-page.component.html',
  styleUrls: ['./cashier-page.component.css']
})
export class CashierPageComponent {
  constructor(public authService: AuthService, private router: Router) {}

  onLoggedIn(): void {}

  goToAdmin(): void {
    this.router.navigate(['/admin']);
  }

  logout(): void {
    this.authService.logout();
  }
}
