import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminUser, AdminUserDto, UserService } from '../../services/user.service';
import { PrintService } from '../../services/print.service';

const SECTION_LABELS: Record<string, string> = {
  'products': 'مدیریت محصولات',
  'categories': 'مدیریت دسته‌بندی',
  'order-type': 'مدیریت انواع سفارش',
  'inventory': 'مدیریت انبار',
  'printers': 'مدیریت پرینترها',
  'pos-devices': 'مدیریت دستگاه‌های POS',
  'payment-methods': 'مدیریت روش‌های پرداخت',
  'style': 'مدیریت استایل',
  'reports': 'گزارشات',
  'users': 'مدیریت کاربران',
  'purchase-sale': 'خرید و فروش',
  'cashier': 'صندوق فروشگاهی',
};

@Component({
  selector: 'app-user-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-admin.component.html',
  styleUrls: ['./user-admin.component.css']
})
export class UserAdminComponent implements OnInit {

  users: AdminUser[] = [];
  sections: string[] = [];
  sectionLabels = SECTION_LABELS;
  searchUser = '';

  get filteredUsers(): AdminUser[] {
    const q = this.searchUser.trim().toLowerCase();
    return q ? this.users.filter(u =>
      u.username.toLowerCase().includes(q) ||
      (u.fullName || '').toLowerCase().includes(q)
    ) : this.users;
  }

  editingId: number | null = null;
  form: AdminUserDto = this.emptyForm();
  error = '';

  constructor(private userService: UserService, private printService: PrintService) {}

  @ViewChild('printArea') printAreaRef?: ElementRef<HTMLElement>;

  printSection(title: string): void {
    if (this.printAreaRef) this.printService.print(title, this.printAreaRef.nativeElement);
  }

  ngOnInit(): void {
    this.loadSections();
    this.loadUsers();
  }

  emptyForm(): AdminUserDto {
    return {
      username: '',
      fullName: '',
      password: '',
      isSuperAdmin: false,
      isActive: true,
      permissions: []
    };
  }

  loadSections(): void {
    this.userService.getSections().subscribe({
      next: (res) => this.sections = res,
      error: (err) => console.error('خطا در دریافت بخش‌ها', err)
    });
  }

  loadUsers(): void {
    this.userService.getUsers().subscribe({
      next: (res) => this.users = res,
      error: (err) => console.error('خطا در دریافت کاربران', err)
    });
  }

  togglePermission(section: string): void {
    const idx = this.form.permissions.indexOf(section);
    if (idx >= 0) {
      this.form.permissions.splice(idx, 1);
    } else {
      this.form.permissions.push(section);
    }
  }

  hasPermission(section: string): boolean {
    return this.form.permissions.includes(section);
  }

  editUser(user: AdminUser): void {
    this.editingId = user.id;
    this.form = {
      username: user.username,
      fullName: user.fullName,
      password: '',
      isSuperAdmin: user.isSuperAdmin,
      isActive: user.isActive,
      permissions: [...user.permissions]
    };
    this.error = '';
  }

  cancelEdit(): void {
    this.editingId = null;
    this.form = this.emptyForm();
    this.error = '';
  }

  save(): void {
    this.error = '';

    if (!this.form.username || (!this.editingId && !this.form.password)) {
      this.error = 'نام کاربری و رمز عبور الزامی است';
      return;
    }

    if (this.editingId) {
      this.userService.updateUser(this.editingId, this.form).subscribe({
        next: () => {
          this.cancelEdit();
          this.loadUsers();
        },
        error: (err) => this.error = err?.error?.message || 'خطا در ویرایش کاربر'
      });
    } else {
      this.userService.createUser(this.form).subscribe({
        next: () => {
          this.cancelEdit();
          this.loadUsers();
        },
        error: (err) => this.error = err?.error?.message || 'خطا در افزودن کاربر'
      });
    }
  }

  deleteUser(id: number): void {
    if (!confirm('این کاربر حذف شود؟')) return;

    this.userService.deleteUser(id).subscribe({
      next: () => this.loadUsers(),
      error: (err) => alert(err?.error?.message || 'خطا در حذف کاربر')
    });
  }
}
