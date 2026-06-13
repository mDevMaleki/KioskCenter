import { Component, OnInit, OnDestroy, ChangeDetectorRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { KioskService } from '../../services/kiosk.service';
//import { PaymentSignalRService } from '../../services/payment-signalr.service';
import { PaymentMethodService, PaymentMethod } from '../../services/payment-method.service';
import { CartItem } from '../../models/kiosk.models';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-payment',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './payment.component.html',
  styleUrls: ['./payment.component.css']
})
export class PaymentComponent implements OnInit, OnDestroy {
  // داده‌های سفارش
  cart: CartItem[] = [];
  subtotal = 0;

  total = 0;
  orderType = '';
  customerName = '';
  
  // وضعیت پرداخت
  paymentSuccess = false;
  orderNumber: number = 0;
  processing = false;
  
  // روش‌های پرداخت
  paymentMethods: PaymentMethod[] = [];
  selectedPaymentMethod: PaymentMethod | null = null;
  
  // برای پرداخت آنلاین
  qrCodeUrl: string | null = null;
  showQrModal = false;
  isGeneratingQr = false;
  paymentVerified = false;
  isCheckingPayment = false;
  public isProcessingPayment = false;
  private currentAuthority: string = '';
  private verificationInterval: any;
  private signalRSubscription: any;
  private stateSubscription?: Subscription;

  // برای POS
  posDeviceConnected: boolean = false;
  isCheckingPos: boolean = false;
  hasActivePosDevice: boolean = false;
  
  errorMessage = '';

  constructor(
    private kioskService: KioskService,
    private paymentMethodService: PaymentMethodService,
    private router: Router,
    private cdr: ChangeDetectorRef,
   // private paymentSignalR: PaymentSignalRService
  ) {}

  ngOnInit() {
    this.loadOrderData();
    this.loadPaymentMethods();
    this.checkPosDevice();
 
 //   this.signalRSubscription = this.paymentSignalR.paymentVerified$.subscribe((data: any) => {
 //     console.log('Payment received via WebSocket!', data);
 //     if (data && data.status === 'success') {
 //       this.paymentVerified = true;
  //      this.clearAllIntervals();
    //    this.cdr.detectChanges();
        
 //       setTimeout(() => {
  ////        this.closeQrModal();
    //      this.paymentSuccess = true;
     //     this.cdr.detectChanges();
          // بعد از موفقیت، پرینت بگیر
      //    this.printReceiptAfterPayment();
       // }, 2000);
      //}
    //});
  }

ngOnDestroy() {
  this.clearAllIntervals();

  if (this.stateSubscription) {
    this.stateSubscription.unsubscribe();
  }

  if (this.signalRSubscription) {
    this.signalRSubscription.unsubscribe();
  }

  if (this.qrCodeUrl) {
    URL.revokeObjectURL(this.qrCodeUrl);
  }
}

  private clearAllIntervals() {
    if (this.verificationInterval) {
      clearInterval(this.verificationInterval);
      this.verificationInterval = null;
    }
  }

  // پرینت فیش بعد از پرداخت موفق
  private printReceiptAfterPayment() {
    if (this.orderNumber) {
      console.log('Printing receipt for order:', this.orderNumber);
      this.kioskService.printReceipt(this.orderNumber).subscribe({
        next: (response) => {
          console.log('Receipt printed successfully:', response);
        },
        error: (err) => {
          console.error('Print error:', err);
          // اگر پرینت خطا داد، یک بار دیگه تلاش کن
          setTimeout(() => {
            this.kioskService.printReceipt(this.orderNumber).subscribe({
              next: (res) => console.log('Retry print success:', res),
              error: (e) => console.error('Retry print failed:', e)
            });
          }, 1000);
        }
      });
    }
  }

  // بارگذاری روش‌های پرداخت فعال
  loadPaymentMethods() {
    this.paymentMethodService.getActiveMethods().subscribe({
      next: (methods) => {
        this.paymentMethods = methods;
        if (this.paymentMethods.length > 0) {
          this.selectedPaymentMethod = this.paymentMethods[0];
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading payment methods:', err);
      }
    });
  }

  // بررسی دستگاه POS
  checkPosDevice() {
    this.isCheckingPos = true;
    this.cdr.detectChanges();
    
    this.kioskService.getDefaultPosDevice().subscribe({
      next: (device: any) => {
        if (device && device.id) {
          this.hasActivePosDevice = true;
          this.kioskService.checkDefaultPosConnection().subscribe({
            next: (conn: any) => {
              this.posDeviceConnected = conn.success;
              this.isCheckingPos = false;
              this.cdr.detectChanges();
            },
            error: (err) => {
              console.error('Error checking POS connection:', err);
              this.posDeviceConnected = false;
              this.isCheckingPos = false;
              this.cdr.detectChanges();
            }
          });
        } else {
          this.hasActivePosDevice = false;
          this.posDeviceConnected = false;
          this.isCheckingPos = false;
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        console.error('Error getting default POS device:', err);
        this.hasActivePosDevice = false;
        this.posDeviceConnected = false;
        this.isCheckingPos = false;
        this.cdr.detectChanges();
      }
    });
  }

  // بررسی اینکه آیا روش پرداخت قابل انتخاب است
  isPaymentMethodAvailable(method: PaymentMethod): boolean {
    if (!method.isActive) return false;
    
    if (method.requiresPosDevice && (!this.hasActivePosDevice || !this.posDeviceConnected)) {
      return false;
    }
    
    return true;
  }

  // انتخاب روش پرداخت
  selectPaymentMethod(method: PaymentMethod) {
    if (!this.isPaymentMethodAvailable(method)) {
      if (method.requiresPosDevice) {
        this.errorMessage = '❌ دستگاه POS در دسترس نیست. لطفاً روش پرداخت دیگری انتخاب کنید.';
        setTimeout(() => {
          this.errorMessage = '';
        }, 3000);
      }
      return;
    }
    
    this.selectedPaymentMethod = method;
    this.errorMessage = '';
    this.qrCodeUrl = null;
  }

 loadOrderData() {
  this.stateSubscription = this.kioskService.state$.subscribe(state => {
    this.cart = state.cart;
    this.orderType = state.orderType === 'EatIn' ? 'داخل سالن' : 'بیرون بر';
    this.customerName = state.customerName;
    this.updateTotals();

    if (this.cart.length === 0 && !this.paymentSuccess && this.router.url.includes('/payment')) {
      this.router.navigate(['/']);
    }
  });
}

  updateTotals() {
    this.subtotal = this.kioskService.getSubtotal();
     // مالیات را صفر می‌کنیم
    this.total = this.subtotal;  // کل مبلغ برابر با جمع سبد خرید بدون مالیات
  }

  goBack() {
    this.kioskService.goBack();
    this.router.navigate(['/products']);
  }

  onCustomerNameInput(event: Event) {
    this.customerName = (event.target as HTMLInputElement).value;
  }

  updateCustomerName() {
    this.kioskService.setCustomerName(this.customerName);
  }

  formatPrice(price: number): string {
    return price.toLocaleString('fa-IR') + ' تومان';
  }

  processPayment() {
    if (this.processing || this.isProcessingPayment) {
      return;
    }
    
    if (this.cart.length === 0) {
      this.errorMessage = 'سبد خرید خالی است!';
      return;
    }
    
    if (!this.selectedPaymentMethod) {
      this.errorMessage = 'لطفاً روش پرداخت را انتخاب کنید!';
      return;
    }
    
    if (!this.isPaymentMethodAvailable(this.selectedPaymentMethod)) {
      this.errorMessage = 'روش پرداخت انتخاب شده در دسترس نیست. لطفاً روش دیگری انتخاب کنید.';
      return;
    }

    this.processing = true;
    this.errorMessage = '';

    switch (this.selectedPaymentMethod.name) {
      case 'Cash':
        this.payWithCash();
        break;
      case 'Pos':
        this.payWithPos();
        break;
      case 'Online':
        this.payWithOnline();
        break;
      default:
        this.payWithCash();
        break;
    }
  }

  payWithCash() {
    this.kioskService.createFullOrder().subscribe({
      next: (order: any) => {
        this.orderNumber = order.id || order.orderId;
        this.paymentSuccess = true;
        this.processing = false;
        this.isProcessingPayment = false;
        
        // پرینت فیش
        this.printReceiptAfterPayment();
        
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.errorMessage = 'خطا در ثبت سفارش';
        this.processing = false;
        this.isProcessingPayment = false;
        this.cdr.detectChanges();
      }
    });
  }

  payWithPos() {
    if (!this.posDeviceConnected) {
      this.errorMessage = '❌ دستگاه POS در دسترس نیست.';
      this.processing = false;
      this.isProcessingPayment = false;
      this.cdr.detectChanges();
      return;
    }

    this.processing = true;
    this.errorMessage = '';

    // مبلغ بدون ضرب در 10 (مالیات حذف شده)
    const amountForPos = this.total;

    this.kioskService.processPaymentWithPosByDevice(amountForPos).subscribe({
      next: (result: any) => {
        console.log('POS payment result:', result);
        
        if (result && result.success) {
          this.kioskService.createFullOrder().subscribe({
            next: (order: any) => {
              this.orderNumber = order.id || order.orderId;
              this.paymentSuccess = true;
              this.processing = false;
              this.isProcessingPayment = false;
              
              this.printReceiptAfterPayment();
              this.cdr.detectChanges();
            },
            error: (err) => {
              console.error('Order creation failed:', err);
              this.errorMessage = 'پرداخت انجام شد اما خطا در ثبت سفارش. لطفاً با پشتیبانی تماس بگیرید.';
              this.processing = false;
              this.isProcessingPayment = false;
              this.cdr.detectChanges();
            }
          });
        } else {
          this.errorMessage = result?.message || 'پرداخت با POS ناموفق بود';
          this.processing = false;
          this.isProcessingPayment = false;
          this.cdr.detectChanges();
        }
      },
      error: (error: any) => {
        console.error('POS payment failed:', error);
        this.errorMessage = error.error?.message || 'خطا در ارتباط با دستگاه POS. لطفاً مجدد تلاش کنید.';
        this.processing = false;
        this.isProcessingPayment = false;
        this.cdr.detectChanges();
      }
    });
  }

  payWithOnline() {
    if (this.showQrModal || this.isProcessingPayment) {
      return;
    }
    
    this.kioskService.createFullOrder().subscribe({
      next: (order: any) => {
        const orderId = order.id || order.orderId;
        this.orderNumber = orderId;
        
        this.isProcessingPayment = true;
        this.showQrModal = true;
        this.isGeneratingQr = true;
        this.paymentVerified = false;
        this.qrCodeUrl = null;
        this.isCheckingPayment = false;
        this.currentAuthority = '';
        this.cdr.detectChanges();
        
        this.kioskService.getOnlinePaymentQr(this.total).subscribe({
          next: (blob: Blob) => {
            const url = URL.createObjectURL(blob);
            this.qrCodeUrl = url;
            this.isGeneratingQr = false;
            this.processing = false;
            this.isProcessingPayment = false;
            
           // this.paymentSignalR.joinOrderGroup(this.orderNumber.toString());
            this.startPaymentVerification();
            this.cdr.detectChanges();
          },
          error: (error: any) => {
            console.error('Online payment failed:', error);
            this.errorMessage = error.message || 'خطا در ایجاد پرداخت آنلاین. لطفاً مجدد تلاش کنید.';
            this.isGeneratingQr = false;
            this.processing = false;
            this.isProcessingPayment = false;
            this.closeQrModal();
            this.cdr.detectChanges();
          }
        });
      },
      error: (err) => {
        console.error('Order creation failed:', err);
        this.errorMessage = 'خطا در ثبت سفارش';
        this.processing = false;
        this.isProcessingPayment = false;
        this.cdr.detectChanges();
      }
    });
  }

  startPaymentVerification() {
    if (this.verificationInterval) {
      clearInterval(this.verificationInterval);
    }
    
    this.verificationInterval = setInterval(() => {
      if (!this.paymentVerified && this.orderNumber) {
        this.kioskService.checkPaymentStatus(this.orderNumber, this.currentAuthority, this.total).subscribe({
          next: (response: any) => {
            if (response && response.success) {
              this.paymentVerified = true;
              this.clearAllIntervals();
              this.cdr.detectChanges();
              
              setTimeout(() => {
                this.closeQrModal();
                this.paymentSuccess = true;
                this.printReceiptAfterPayment();  // پرینت بعد از پرداخت
                this.cdr.detectChanges();
              }, 2000);
            }
          },
          error: (err) => {
            console.error('Payment verification error:', err);
          }
        });
      }
    }, 5000);
  }

  checkPaymentManually() {
    if (this.isCheckingPayment || this.paymentVerified) {
      return;
    }
    
    this.isCheckingPayment = true;
    this.errorMessage = '';
    this.cdr.detectChanges();
    
    this.kioskService.checkPaymentStatus(this.orderNumber, this.currentAuthority, this.total).subscribe({
      next: (response: any) => {
        this.isCheckingPayment = false;
        
        if (response && response.success) {
          this.paymentVerified = true;
          this.clearAllIntervals();
          this.cdr.detectChanges();
          
          setTimeout(() => {
            this.closeQrModal();
            this.paymentSuccess = true;
            this.printReceiptAfterPayment();
            this.cdr.detectChanges();
          }, 2000);
        } else {
          this.errorMessage = response?.message || 'پرداخت هنوز انجام نشده است. لطفاً ابتدا QR را اسکن کنید.';
          this.cdr.detectChanges();
          
          setTimeout(() => {
            if (this.errorMessage) {
              this.errorMessage = '';
              this.cdr.detectChanges();
            }
          }, 3000);
        }
      },
      error: (error: any) => {
        this.isCheckingPayment = false;
        this.errorMessage = 'خطا در بررسی وضعیت پرداخت. لطفاً دوباره تلاش کنید.';
        this.cdr.detectChanges();
        
        setTimeout(() => {
          if (this.errorMessage) {
            this.errorMessage = '';
            this.cdr.detectChanges();
          }
        }, 3000);
      }
    });
  }

  refreshQrCode() {
    if (this.isGeneratingQr) return;
    
    if (this.qrCodeUrl) {
      URL.revokeObjectURL(this.qrCodeUrl);
    }
    
    this.isGeneratingQr = true;
    this.paymentVerified = false;
    this.isCheckingPayment = false;
    this.errorMessage = '';
    this.cdr.detectChanges();
    
    this.kioskService.getOnlinePaymentQr(this.total).subscribe({
      next: (blob: Blob) => {
        const url = URL.createObjectURL(blob);
        this.qrCodeUrl = url;
        this.isGeneratingQr = false;
        this.cdr.detectChanges();
      },
      error: (error: any) => {
        console.error('Refresh QR failed:', error);
        this.errorMessage = 'خطا در دریافت مجدد QR Code';
        this.isGeneratingQr = false;
        this.cdr.detectChanges();
      }
    });
  }

  closeQrModal() {
    console.log('Closing QR modal');
    this.clearAllIntervals();
    this.showQrModal = false;
    
    if (this.qrCodeUrl) {
      URL.revokeObjectURL(this.qrCodeUrl);
    }
    this.qrCodeUrl = null;
    this.paymentVerified = false;
    this.isGeneratingQr = false;
    this.isProcessingPayment = false;
    this.processing = false;
    this.isCheckingPayment = false;
    this.currentAuthority = '';
    this.errorMessage = '';
    this.cdr.detectChanges();
  }

  newOrder() {
    this.paymentSuccess = false;
    this.orderNumber = 0;
    this.cart = [];
    this.errorMessage = '';
    if (this.qrCodeUrl) {
      URL.revokeObjectURL(this.qrCodeUrl);
    }
    this.qrCodeUrl = null;
    this.showQrModal = false;
    this.paymentVerified = false;
    this.isProcessingPayment = false;
    this.processing = false;
    this.isCheckingPayment = false;
    this.currentAuthority = '';
    this.clearAllIntervals();
    this.kioskService.reset();
    this.router.navigate(['/']);
  }

  @HostListener('window:message', ['$event'])
  onWindowMessage(event: MessageEvent) {
    if (event.data && event.data.type === 'PAYMENT_SUCCESS') {
      console.log('Payment success message received from popup:', event.data);
      this.paymentVerified = true;
      this.clearAllIntervals();
      this.cdr.detectChanges();
      
      setTimeout(() => {
        this.closeQrModal();
        this.paymentSuccess = true;
        this.printReceiptAfterPayment();
        this.cdr.detectChanges();
      }, 2000);
    }
  }
}