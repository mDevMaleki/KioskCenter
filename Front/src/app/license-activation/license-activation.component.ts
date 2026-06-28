import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LicenseService } from '../services/license.service';

@Component({
  selector: 'app-license-activation',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './license-activation.component.html',
  styleUrls: ['./license-activation.component.css']
})
export class LicenseActivationComponent implements OnInit {

  loading = true;
  message = '';
  hardwareHash = '';
  copied = false;

  selectedFile: File | null = null;
  uploading = false;
  uploadError = '';
  uploadSuccess = false;

  constructor(private licenseService: LicenseService) {}

  ngOnInit(): void {
    this.checkStatus();
  }

  checkStatus(): void {
    this.loading = true;
    this.licenseService.getStatus().subscribe({
      next: (res) => {
        this.message = res.message;
        this.hardwareHash = res.hardwareHash;
        this.loading = false;
      },
      error: () => {
        this.message = 'خطا در ارتباط با سرور. لطفاً از اجرای برنامه مطمئن شوید.';
        this.loading = false;
      }
    });
  }

  copyHardwareId(): void {
    if (!this.hardwareHash) return;
    navigator.clipboard.writeText(this.hardwareHash).then(() => {
      this.copied = true;
      setTimeout(() => this.copied = false, 2000);
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files && input.files.length > 0 ? input.files[0] : null;
    this.uploadError = '';
    this.uploadSuccess = false;
  }

  uploadLicense(): void {
    if (!this.selectedFile) {
      this.uploadError = 'لطفاً فایل لایسنس را انتخاب کنید';
      return;
    }

    this.uploading = true;
    this.uploadError = '';

    this.licenseService.uploadLicense(this.selectedFile).subscribe({
      next: () => {
        this.uploading = false;
        this.uploadSuccess = true;
        setTimeout(() => window.location.reload(), 1500);
      },
      error: (err) => {
        this.uploading = false;
        this.uploadError = err?.error?.message || 'فایل لایسنس نامعتبر است';
      }
    });
  }
}
