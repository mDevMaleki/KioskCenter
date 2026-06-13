import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PosDeviceService } from '../../services/pos-device.service';
import { PosDevice } from '../../models/pos-device.model';

@Component({
  selector: 'app-pos-device-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pos-device-admin.component.html',
  styleUrls: ['./pos-device-admin.component.css']
})
export class PosDeviceAdminComponent implements OnInit {
  devices: PosDevice[] = [];
  editingDevice: PosDevice | null = null;
  showForm = false;
  isEditing = false;
  isLoading = false;
  paymentAmount: number = 0;
  selectedDeviceId: number | null = null;
  paymentResult: any = null;

  deviceTypes = [
    { value: 'Parsian', label: 'پارسیان' },
    { value: 'PardakhtNovin', label: 'پرداخت نوین' }
  ];

  constructor(private posDeviceService: PosDeviceService) {}

  ngOnInit() {
    this.loadDevices();
  }

  loadDevices() {
    this.isLoading = true;
    this.posDeviceService.getDevices().subscribe({
      next: (res) => {
        this.devices = res;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading devices:', err);
        this.isLoading = false;
      }
    });
  }

  addDevice() {
    this.editingDevice = {
      id: 0,
      name: '',
      type: 'Parsian',
      ipAddress: '192.168.1.3',
      port: 1362,
      isActive: true,
      isDefault: false,
      priority: this.devices.length,
      createdAt: new Date()
    };
    this.isEditing = false;
    this.showForm = true;
  }

  editDevice(device: PosDevice) {
    this.editingDevice = { ...device };
    this.isEditing = true;
    this.showForm = true;
  }

  deleteDevice(id: number) {
    if (confirm('آیا از حذف این دستگاه اطمینان دارید؟')) {
      this.posDeviceService.deleteDevice(id).subscribe({
        next: () => this.loadDevices(),
        error: (err) => console.error('Error deleting device:', err)
      });
    }
  }

  saveDevice() {
    if (!this.editingDevice) return;

    if (this.isEditing) {
      this.posDeviceService.updateDevice(this.editingDevice.id, this.editingDevice).subscribe({
        next: () => {
          this.loadDevices();
          this.showForm = false;
          this.editingDevice = null;
        },
        error: (err) => console.error('Error updating device:', err)
      });
    } else {
      this.posDeviceService.addDevice(this.editingDevice).subscribe({
        next: () => {
          this.loadDevices();
          this.showForm = false;
          this.editingDevice = null;
        },
        error: (err) => console.error('Error adding device:', err)
      });
    }
  }

  cancelForm() {
    this.showForm = false;
    this.editingDevice = null;
  }

  testConnection(deviceId: number) {
    this.posDeviceService.checkConnection(deviceId).subscribe({
      next: (res) => {
        alert(res.message);
      },
      error: (err) => {
        alert('خطا در بررسی اتصال');
      }
    });
  }

  startPayment() {
    if (this.paymentAmount <= 0) {
      alert('لطفاً مبلغ معتبر وارد کنید');
      return;
    }

    this.isLoading = true;
    this.paymentResult = null;

    this.posDeviceService.pay({
      amount: this.paymentAmount,
      deviceId: this.selectedDeviceId || undefined
    }).subscribe({
      next: (res) => {
        this.paymentResult = res;
        this.isLoading = false;
        if (res.success) {
          alert('پرداخت با موفقیت انجام شد');
        } else {
          alert('خطا در پرداخت: ' + res.message);
        }
      },
      error: (err) => {
        console.error('Error during payment:', err);
        this.paymentResult = { success: false, message: 'خطا در ارتباط با سرور' };
        this.isLoading = false;
      }
    });
  }

  setDefault(device: PosDevice) {
    // ریست کردن default قبلی
    this.devices.forEach(d => {
      d.isDefault = false;
      if (d.id === device.id) {
        d.isDefault = true;
      }
      this.posDeviceService.updateDevice(d.id, d).subscribe();
    });
  }
}