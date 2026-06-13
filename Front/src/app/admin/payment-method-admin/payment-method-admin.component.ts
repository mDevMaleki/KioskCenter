import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaymentMethodService, PaymentMethod } from '../../services/payment-method.service';

@Component({
  selector: 'app-payment-method-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payment-method-admin.component.html',
  styleUrls: ['./payment-method-admin.component.css']
})
export class PaymentMethodAdminComponent implements OnInit {
  paymentMethods: PaymentMethod[] = [];
  editingMethod: PaymentMethod | null = null;
  showForm = false;
  isEditing = false;
  isLoading = false;

  // گزینه‌های آیکون
  iconOptions = [
    { value: '💵', label: '💵 پول نقد' },
    { value: '💳', label: '💳 کارت بانکی' },
    { value: '📱', label: '📱 موبایل' },
    { value: '🏦', label: '🏦 بانک' },
    { value: '🪙', label: '🪙 سکه' },
    { value: '🎫', label: '🎫 بن تخفیف' },
    { value: '💸', label: '💸 انتقال' }
  ];

  constructor(private paymentMethodService: PaymentMethodService) {}

  ngOnInit() {
    this.loadPaymentMethods();
  }

  loadPaymentMethods() {
    this.isLoading = true;
    this.paymentMethodService.getAllMethods().subscribe({
      next: (res) => {
        this.paymentMethods = res;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading payment methods:', err);
        this.isLoading = false;
      }
    });
  }

  addMethod() {
    this.editingMethod = {
      id: 0,
      name: '',
      title: '',
      icon: '💵',
      description: '',
      isActive: true,
      sortOrder: this.paymentMethods.length,
      requiresPosDevice: false
      // createdAt و updatedAt حذف شدند
    };
    this.isEditing = false;
    this.showForm = true;
  }

  editMethod(method: PaymentMethod) {
    this.editingMethod = { ...method };
    this.isEditing = true;
    this.showForm = true;
  }

  deleteMethod(id: number) {
    if (confirm('آیا از حذف این روش پرداخت اطمینان دارید؟')) {
      this.paymentMethodService.deleteMethod(id).subscribe({
        next: () => {
          this.loadPaymentMethods();
        },
        error: (err) => {
          console.error('Error deleting payment method:', err);
          alert('خطا در حذف روش پرداخت');
        }
      });
    }
  }

  saveMethod() {
    if (!this.editingMethod) return;

    if (this.isEditing) {
      this.paymentMethodService.updateMethod(this.editingMethod.id, this.editingMethod).subscribe({
        next: () => {
          this.loadPaymentMethods();
          this.showForm = false;
          this.editingMethod = null;
          alert('روش پرداخت با موفقیت ویرایش شد');
        },
        error: (err) => {
          console.error('Error updating payment method:', err);
          alert('خطا در ویرایش روش پرداخت');
        }
      });
    } else {
      this.paymentMethodService.createMethod(this.editingMethod).subscribe({
        next: () => {
          this.loadPaymentMethods();
          this.showForm = false;
          this.editingMethod = null;
          alert('روش پرداخت با موفقیت اضافه شد');
        },
        error: (err) => {
          console.error('Error creating payment method:', err);
          alert('خطا در ایجاد روش پرداخت');
        }
      });
    }
  }

  cancelForm() {
    this.showForm = false;
    this.editingMethod = null;
  }

  toggleStatus(method: PaymentMethod) {
    method.isActive = !method.isActive;
    this.paymentMethodService.updateMethod(method.id, method).subscribe({
      next: () => {
        this.loadPaymentMethods();
      },
      error: (err) => {
        console.error('Error toggling status:', err);
        method.isActive = !method.isActive;
      }
    });
  }

  seedDefaultMethods() {
    if (confirm('آیا از افزودن روش‌های پرداخت پیش‌فرض اطمینان دارید؟')) {
      this.paymentMethodService.seedMethods().subscribe({
        next: () => {
          this.loadPaymentMethods();
          alert('روش‌های پرداخت پیش‌فرض اضافه شدند');
        },
        error: (err) => {
          console.error('Error seeding methods:', err);
          alert('خطا در افزودن روش‌های پیش‌فرض');
        }
      });
    }
  }
}