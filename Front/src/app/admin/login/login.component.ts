import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-admin-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  @Output() loggedIn = new EventEmitter<void>();

  username = '';
  password = '';
  error = '';
  loading = false;

  constructor(private authService: AuthService) {}

  submit(): void {
    if (!this.username) {
      this.error = 'نام کاربری و رمز عبور را وارد کنید';
      return;
    }

    this.loading = true;
    this.error = '';

    this.authService.login(this.username, this.password).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success) {
          this.loggedIn.emit();
        }
      },
      error: (err) => {
        this.loading = false;
        this.error = err?.error?.message || 'نام کاربری یا رمز عبور اشتباه است';
      }
    });
  }
}
