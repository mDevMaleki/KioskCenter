import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { KioskService } from '../../services/kiosk.service';
import { JalaliDatePipe } from '../../pipes/jalali-date.pipe';
import { JalaliCalendarService } from '../../services/jalali-calendar.service';

interface TopProduct {
  name: string;
  quantity: number;
  total: number;
}

interface HourlyDistribution {
  [hour: string]: number;
}

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule, JalaliDatePipe],
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.css']
})
export class ReportsComponent implements OnInit {
  
  orders: any[] = [];
  filteredOrders: any[] = [];
  totalSales: number = 0;
  totalOrders: number = 0;
  totalTax: number = 0;
  averageOrderValue: number = 0;

  // فیلترهای تاریخ شمسی
  filterType: 'daily' | 'weekly' | 'monthly' | 'yearly' | 'custom' = 'daily';
  
  // تاریخ شمسی امروز
  todayJalali: string = '';
  
  // برای فیلتر روزانه
  selectedDate: string = '';
  
  // برای فیلتر هفتگی
  selectedWeek: number = 1;
  selectedYearForWeek: number = 1403;
  weekTitle: string = '';
  
  // برای فیلتر ماهانه
  selectedMonth: number = 1;
  selectedYearForMonth: number = 1403;
  
  // برای فیلتر سالانه
  selectedYear: number = 1403;
  
  // برای فیلتر دلخواه
  startDate: string = '';
  endDate: string = '';

  // آمار پیشرفته
  stats = {
    eatInOrders: 0,
    takeAwayOrders: 0,
    topProducts: [] as TopProduct[],
    hourlyDistribution: {} as HourlyDistribution
  };

  // لیست ماه‌های شمسی
  months = [
    { value: 1, name: 'فروردین' },
    { value: 2, name: 'اردیبهشت' },
    { value: 3, name: 'خرداد' },
    { value: 4, name: 'تیر' },
    { value: 5, name: 'مرداد' },
    { value: 6, name: 'شهریور' },
    { value: 7, name: 'مهر' },
    { value: 8, name: 'آبان' },
    { value: 9, name: 'آذر' },
    { value: 10, name: 'دی' },
    { value: 11, name: 'بهمن' },
    { value: 12, name: 'اسفند' }
  ];

  weeks: { value: number; name: string }[] = [];

  constructor(
    private kioskService: KioskService,
    private jalaliCalendar: JalaliCalendarService
  ) {
    this.generateWeeksList();
    this.initializeDates();
  }

  ngOnInit(): void {
    this.testJalaliConversion();
    this.loadOrders();
  }

  // مقداردهی اولیه تاریخ‌ها
  initializeDates() {
    this.todayJalali = this.jalaliCalendar.getTodayJalali();
    this.selectedDate = this.todayJalali;
    this.selectedYear = this.jalaliCalendar.getCurrentJalaliYear();
    this.selectedYearForWeek = this.selectedYear;
    this.selectedYearForMonth = this.selectedYear;
    this.selectedMonth = this.jalaliCalendar.getCurrentJalaliMonth();
    this.selectedWeek = this.getCurrentWeekJalali();
    this.startDate = this.getJalaliDateDaysAgo(7);
    this.endDate = this.todayJalali;
    this.updateWeekTitle();
  }

  // تست تبدیل تاریخ
  testJalaliConversion() {
    const now = new Date();
    console.log('=== تست تبدیل تاریخ ===');
    console.log('تاریخ میلادی امروز:', now);
    console.log('تاریخ شمسی امروز:', this.jalaliCalendar.getTodayJalali());
    console.log('سال شمسی:', this.jalaliCalendar.getCurrentJalaliYear());
    console.log('ماه شمسی:', this.jalaliCalendar.getCurrentJalaliMonth());
    console.log('روز شمسی:', this.jalaliCalendar.getCurrentJalaliDay());
  }

  // دریافت شماره هفته جاری
  getCurrentWeekJalali(): number {
    const now = new Date();
    const startOfYear = new Date(now.getFullYear(), 0, 1);
    const days = Math.floor((now.getTime() - startOfYear.getTime()) / 86400000);
    return Math.floor(days / 7) + 1;
  }

  // دریافت تاریخ شمسی چند روز قبل
  getJalaliDateDaysAgo(days: number): string {
    const date = new Date();
    date.setDate(date.getDate() - days);
    return this.jalaliCalendar.formatJalali(date, false);
  }

  // تولید لیست هفته‌ها
  generateWeeksList() {
    for (let i = 1; i <= 52; i++) {
      this.weeks.push({ value: i, name: `هفته ${i}` });
    }
  }

  // به‌روزرسانی عنوان هفته
  updateWeekTitle() {
    this.weekTitle = this.jalaliCalendar.getWeekTitle(this.selectedYearForWeek, this.selectedWeek);
  }

  // تبدیل تاریخ شمسی به میلادی (برای ارسال به API)
  jalaliToGregorianDate(jalaliDate: string): Date {
    return this.jalaliCalendar.parseJalali(jalaliDate);
  }

  // دریافت محدوده تاریخ هفته شمسی
  getJalaliWeekRange(year: number, week: number): { start: string; end: string } {
    const firstDayOfYear = this.jalaliCalendar.parseJalali(`${year}/01/01`);
    const startDate = new Date(firstDayOfYear);
    startDate.setDate(firstDayOfYear.getDate() + (week - 1) * 7);
    const endDate = new Date(startDate);
    endDate.setDate(startDate.getDate() + 6);
    
    return {
      start: this.jalaliCalendar.formatJalali(startDate, false),
      end: this.jalaliCalendar.formatJalali(endDate, false)
    };
  }

  // دریافت تعداد روزهای ماه شمسی
  getJalaliMonthDays(year: number, month: number): number {
    if (month <= 6) return 31;
    if (month <= 11) return 30;
    const lastDay = this.jalaliCalendar.parseJalali(`${year}/12/31`);
    const isLeap = lastDay.getMonth() === 11 && lastDay.getDate() === 30;
    return isLeap ? 30 : 29;
  }

  // بارگذاری سفارشات بر اساس نوع فیلتر
  loadOrders() {
    switch (this.filterType) {
      case 'daily':
        this.loadDailyOrders();
        break;
      case 'weekly':
        this.loadWeeklyOrders();
        break;
      case 'monthly':
        this.loadMonthlyOrders();
        break;
      case 'yearly':
        this.loadYearlyOrders();
        break;
      case 'custom':
        this.loadCustomRangeOrders();
        break;
    }
  }

  // بارگذاری سفارشات روزانه
  loadDailyOrders() {
    const dateStr = this.jalaliCalendar.getDateStringForAPI(this.selectedDate);
    console.log('تاریخ شمسی:', this.selectedDate, 'تاریخ ارسال:', dateStr);
    
    this.kioskService.getOrdersByDate(dateStr).subscribe({
      next: (res: any[]) => {
        this.orders = res;
        this.filteredOrders = res;
        this.calculateStats();
      },
      error: (error) => {
        console.error('Error loading daily orders:', error);
        this.orders = [];
        this.filteredOrders = [];
        this.calculateStats();
      }
    });
  }

  // بارگذاری سفارشات هفتگی
  loadWeeklyOrders() {
    this.updateWeekTitle();
    const weekRange = this.getJalaliWeekRange(this.selectedYearForWeek, this.selectedWeek);
    const startGregorian = this.jalaliToGregorianDate(weekRange.start);
    const endGregorian = this.jalaliToGregorianDate(weekRange.end);
    
    this.kioskService.getOrdersByDateRange(
      startGregorian.toISOString().split('T')[0],
      endGregorian.toISOString().split('T')[0]
    ).subscribe({
      next: (res: any[]) => {
        this.orders = res;
        this.filteredOrders = res;
        this.calculateStats();
      },
      error: (error) => {
        console.error('Error loading weekly orders:', error);
        this.orders = [];
        this.filteredOrders = [];
        this.calculateStats();
      }
    });
  }

  // بارگذاری سفارشات ماهانه
  loadMonthlyOrders() {
    const daysInMonth = this.getJalaliMonthDays(this.selectedYearForMonth, this.selectedMonth);
    const firstDayJalali = `${this.selectedYearForMonth}/${this.selectedMonth}/01`;
    const lastDayJalali = `${this.selectedYearForMonth}/${this.selectedMonth}/${daysInMonth}`;
    
    const startGregorian = this.jalaliToGregorianDate(firstDayJalali);
    const endGregorian = this.jalaliToGregorianDate(lastDayJalali);
    
    this.kioskService.getOrdersByDateRange(
      startGregorian.toISOString().split('T')[0],
      endGregorian.toISOString().split('T')[0]
    ).subscribe({
      next: (res: any[]) => {
        this.orders = res;
        this.filteredOrders = res;
        this.calculateStats();
      },
      error: (error) => {
        console.error('Error loading monthly orders:', error);
        this.orders = [];
        this.filteredOrders = [];
        this.calculateStats();
      }
    });
  }

  // بارگذاری سفارشات سالانه
  loadYearlyOrders() {
    const daysInLastMonth = this.getJalaliMonthDays(this.selectedYear, 12);
    const firstDayJalali = `${this.selectedYear}/01/01`;
    const lastDayJalali = `${this.selectedYear}/12/${daysInLastMonth}`;
    
    const startGregorian = this.jalaliToGregorianDate(firstDayJalali);
    const endGregorian = this.jalaliToGregorianDate(lastDayJalali);
    
    this.kioskService.getOrdersByDateRange(
      startGregorian.toISOString().split('T')[0],
      endGregorian.toISOString().split('T')[0]
    ).subscribe({
      next: (res: any[]) => {
        this.orders = res;
        this.filteredOrders = res;
        this.calculateStats();
      },
      error: (error) => {
        console.error('Error loading yearly orders:', error);
        this.orders = [];
        this.filteredOrders = [];
        this.calculateStats();
      }
    });
  }

  // بارگذاری سفارشات با بازه دلخواه
  loadCustomRangeOrders() {
    const startGregorian = this.jalaliToGregorianDate(this.startDate);
    const endGregorian = this.jalaliToGregorianDate(this.endDate);
    
    this.kioskService.getOrdersByDateRange(
      startGregorian.toISOString().split('T')[0],
      endGregorian.toISOString().split('T')[0]
    ).subscribe({
      next: (res: any[]) => {
        this.orders = res;
        this.filteredOrders = res;
        this.calculateStats();
      },
      error: (error) => {
        console.error('Error loading custom range orders:', error);
        this.orders = [];
        this.filteredOrders = [];
        this.calculateStats();
      }
    });
  }

  // محاسبه آمار
  calculateStats() {
    this.totalOrders = this.filteredOrders.length;
    this.totalSales = this.filteredOrders.reduce((sum, order) => sum + (order.totalAmount || 0), 0);
    this.totalTax = this.filteredOrders.reduce((sum, order) => sum + (order.taxAmount || 0), 0);
    this.averageOrderValue = this.totalOrders > 0 ? this.totalSales / this.totalOrders : 0;

    this.stats.eatInOrders = this.filteredOrders.filter(o => o.orderType === 'EatIn').length;
    this.stats.takeAwayOrders = this.filteredOrders.filter(o => o.orderType === 'TakeAway').length;

    const hourlyDist: HourlyDistribution = {};
    this.filteredOrders.forEach(order => {
      const hour = new Date(order.orderDate).getHours();
      const hourKey = hour.toString();
      hourlyDist[hourKey] = (hourlyDist[hourKey] || 0) + (order.totalAmount || 0);
    });
    this.stats.hourlyDistribution = hourlyDist;

    const productSales: { [key: string]: TopProduct } = {};
    this.filteredOrders.forEach(order => {
      if (order.orderItems && Array.isArray(order.orderItems)) {
        order.orderItems.forEach((item: any) => {
          const productName = item.product?.name || `محصول ${item.productId}`;
          if (!productSales[productName]) {
            productSales[productName] = { name: productName, quantity: 0, total: 0 };
          }
          productSales[productName].quantity += item.quantity;
          productSales[productName].total += item.totalPrice || 0;
        });
      }
    });
    this.stats.topProducts = Object.values(productSales)
      .sort((a, b) => b.total - a.total)
      .slice(0, 5);
  }

  getPercentage(value: number, total: number): number {
    if (total === 0) return 0;
    return (value / total) * 100;
  }

  get hourlyDistributionArray(): { hour: string; value: number }[] {
    return Object.entries(this.stats.hourlyDistribution)
      .map(([hour, value]) => ({ hour, value: value as number }))
      .sort((a, b) => parseInt(a.hour) - parseInt(b.hour));
  }

  // ناوبری روزانه
  previousDay() {
    const currentDate = this.jalaliCalendar.parseJalali(this.selectedDate);
    currentDate.setDate(currentDate.getDate() - 1);
    this.selectedDate = this.jalaliCalendar.formatJalali(currentDate, false);
    this.loadDailyOrders();
  }

  nextDay() {
    const currentDate = this.jalaliCalendar.parseJalali(this.selectedDate);
    currentDate.setDate(currentDate.getDate() + 1);
    this.selectedDate = this.jalaliCalendar.formatJalali(currentDate, false);
    this.loadDailyOrders();
  }

  goToToday() {
    this.selectedDate = this.jalaliCalendar.getTodayJalali();
    this.loadDailyOrders();
  }

  // ناوبری هفتگی
  previousWeek() {
    if (this.selectedWeek > 1) {
      this.selectedWeek--;
    } else {
      this.selectedWeek = 52;
      this.selectedYearForWeek--;
    }
    this.updateWeekTitle();
    this.loadWeeklyOrders();
  }

  nextWeek() {
    if (this.selectedWeek < 52) {
      this.selectedWeek++;
    } else {
      this.selectedWeek = 1;
      this.selectedYearForWeek++;
    }
    this.updateWeekTitle();
    this.loadWeeklyOrders();
  }

  goToCurrentWeek() {
    this.selectedYearForWeek = this.jalaliCalendar.getCurrentJalaliYear();
    this.selectedWeek = this.getCurrentWeekJalali();
    this.updateWeekTitle();
    this.loadWeeklyOrders();
  }

  // رویدادهای تغییر فیلترها
  onFilterTypeChange() {
    this.loadOrders();
  }

  onDateChange() {
    this.loadDailyOrders();
  }

  onWeekChange() {
    this.updateWeekTitle();
    this.loadWeeklyOrders();
  }

  onMonthChange() {
    this.loadMonthlyOrders();
  }

  onYearChange() {
    if (this.filterType === 'weekly') {
      this.updateWeekTitle();
      this.loadWeeklyOrders();
    } else if (this.filterType === 'monthly') {
      this.loadMonthlyOrders();
    } else if (this.filterType === 'yearly') {
      this.loadYearlyOrders();
    }
  }
  // اضافه کنید به ReportsComponent

printReport() {
  if (this.filteredOrders.length === 0) {
    alert('هیچ سفارشی برای چاپ وجود ندارد.');
    return;
  }

  // آماده‌سازی عنوان دوره
  let period = '';
  switch (this.filterType) {
    case 'daily':
      period = `گزارش روزانه - تاریخ ${this.selectedDate}`;
      break;
    case 'weekly':
      period = `گزارش هفتگی - ${this.weekTitle}`;
      break;
    case 'monthly':
      const monthName = this.months.find(m => m.value === this.selectedMonth)?.name || '';
      period = `گزارش ماهانه - ${monthName} ${this.selectedYearForMonth}`;
      break;
    case 'yearly':
      period = `گزارش سالانه - سال ${this.selectedYear}`;
      break;
    case 'custom':
      period = `گزارش دلخواه - از ${this.startDate} تا ${this.endDate}`;
      break;
  }

  // تبدیل سفارشات به فرمت مورد نیاز API
  const ordersForPrint = this.filteredOrders.map(order => ({
    orderNumber: order.orderNumber,
    customerName: order.customerName || 'مهمان',
    orderDate: order.orderDate,
    orderType: order.orderType,
    totalAmount: order.totalAmount,
    taxAmount: order.taxAmount,
    paymentStatus: order.paymentStatus,
    orderItems: order.orderItems || []
  }));

  this.kioskService.printReport(
    ordersForPrint,
    'گزارش فروش',
    period,
    this.totalSales,
    this.totalTax,
    this.totalOrders,
    this.averageOrderValue,
    true  // true = پرینت مستقیم، false = دانلود PDF
  ).subscribe({
    next: (res: any) => {
      if (res.success) {
        alert('✓ ' + res.message);
      }
    },
    error: (error) => {
      console.error('Error printing report:', error);
      alert('✗ خطا در چاپ گزارش');
    }
  });
}
// متد پرینت فیش (هم برای چاپ اول و هم برای پرینت مجدد)
printOrder(orderId: number) {
  this.kioskService.printReceipt(orderId).subscribe({
    next: (res: any) => {
      if (res.success) {
        alert('✓ ' + (res.message || 'فیش با موفقیت چاپ شد'));
      } else {
        alert('✗ ' + (res.message || 'خطا در چاپ فیش'));
      }
    },
    error: (error) => {
      console.error('Error printing receipt:', error);
      alert('✗ خطا در چاپ فیش');
    }
  });
}
  onCustomRangeChange() {
    this.loadCustomRangeOrders();
  }



  exportToExcel() {
    console.log('Export to Excel');
  }
}