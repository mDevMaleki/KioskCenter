import { Injectable } from '@angular/core';

// @ts-ignore - برای جلوگیری از خطای TypeScript
import * as jalaali from 'jalaali-js';

@Injectable({
  providedIn: 'root'
})
export class JalaliCalendarService {
  
  // فرمت کردن تاریخ میلادی به شمسی
  formatJalali(date: Date, withTime: boolean = false): string {
    try {
      // اصلاح منطقه زمانی
      const utcDate = new Date(Date.UTC(
        date.getFullYear(),
        date.getMonth(),
        date.getDate(),
        12, 0, 0
      ));
      
      const jDate = jalaali.toJalaali(
        utcDate.getUTCFullYear(),
        utcDate.getUTCMonth() + 1,
        utcDate.getUTCDate()
      );
      
      const dateStr = `${jDate.jy}/${jDate.jm.toString().padStart(2, '0')}/${jDate.jd.toString().padStart(2, '0')}`;
      
      if (withTime) {
        const hours = date.getHours().toString().padStart(2, '0');
        const minutes = date.getMinutes().toString().padStart(2, '0');
        return `${dateStr} ${hours}:${minutes}`;
      }
      
      return dateStr;
    } catch (error) {
      console.error('Error formatting jalali date:', error);
      return '';
    }
  }
  
  // دریافت تاریخ شمسی امروز
  getTodayJalali(): string {
    const now = new Date();
    return this.formatJalali(now, false);
  }
  
  // دریافت سال شمسی جاری
  getCurrentJalaliYear(): number {
    try {
      const now = new Date();
      const utcDate = new Date(Date.UTC(
        now.getFullYear(),
        now.getMonth(),
        now.getDate(),
        12, 0, 0
      ));
      const jDate = jalaali.toJalaali(
        utcDate.getUTCFullYear(),
        utcDate.getUTCMonth() + 1,
        utcDate.getUTCDate()
      );
      return jDate.jy;
    } catch (error) {
      console.error('Error getting current year:', error);
      return 1405;
    }
  }
  
  // دریافت ماه شمسی جاری
  getCurrentJalaliMonth(): number {
    try {
      const now = new Date();
      const utcDate = new Date(Date.UTC(
        now.getFullYear(),
        now.getMonth(),
        now.getDate(),
        12, 0, 0
      ));
      const jDate = jalaali.toJalaali(
        utcDate.getUTCFullYear(),
        utcDate.getUTCMonth() + 1,
        utcDate.getUTCDate()
      );
      return jDate.jm;
    } catch (error) {
      console.error('Error getting current month:', error);
      return 1;
    }
  }
  
  // دریافت روز شمسی جاری
  getCurrentJalaliDay(): number {
    try {
      const now = new Date();
      const utcDate = new Date(Date.UTC(
        now.getFullYear(),
        now.getMonth(),
        now.getDate(),
        12, 0, 0
      ));
      const jDate = jalaali.toJalaali(
        utcDate.getUTCFullYear(),
        utcDate.getUTCMonth() + 1,
        utcDate.getUTCDate()
      );
      return jDate.jd;
    } catch (error) {
      console.error('Error getting current day:', error);
      return 1;
    }
  }
  
  // تبدیل تاریخ شمسی به میلادی (اصلاح شده برای رفع اختلاف یک روز)
  parseJalali(jalaliDate: string): Date {
    try {
      const parts = jalaliDate.split(/[\/\-]/);
      if (parts.length === 3) {
        const year = parseInt(parts[0]);
        const month = parseInt(parts[1]);
        const day = parseInt(parts[2]);
        
        // تبدیل به میلادی
        let gregorian = jalaali.toGregorian(year, month, day);
        
        // ایجاد تاریخ با تنظیم ساعت 12:00 برای جلوگیری از اختلاف منطقه زمانی
        let resultDate = new Date(Date.UTC(gregorian.gy, gregorian.gm - 1, gregorian.gd, 12, 0, 0));
        
        // بررسی و اصلاح اختلاف یک روز
        const checkJalali = this.formatJalali(resultDate, false);
        if (checkJalali !== jalaliDate) {
          // اگر اختلاف داشت، یک روز جابجا کن
          if (checkJalali < jalaliDate) {
            resultDate = new Date(Date.UTC(gregorian.gy, gregorian.gm - 1, gregorian.gd + 1, 12, 0, 0));
          } else {
            resultDate = new Date(Date.UTC(gregorian.gy, gregorian.gm - 1, gregorian.gd - 1, 12, 0, 0));
          }
        }
        
        return resultDate;
      }
      return new Date();
    } catch (error) {
      console.error('Error parsing jalali date:', error);
      return new Date();
    }
  }
  
  // دریافت محدوده هفته
  getWeekRange(year: number, week: number): { start: string; end: string } {
    try {
      const firstDayOfYear = this.parseJalali(`${year}/01/01`);
      const startDate = new Date(firstDayOfYear);
      startDate.setUTCDate(firstDayOfYear.getUTCDate() + (week - 1) * 7);
      const endDate = new Date(startDate);
      endDate.setUTCDate(startDate.getUTCDate() + 6);
      
      return {
        start: this.formatJalali(startDate, false),
        end: this.formatJalali(endDate, false)
      };
    } catch (error) {
      console.error('Error getting week range:', error);
      return { start: `${year}/01/01`, end: `${year}/01/07` };
    }
  }
  
  // دریافت تعداد روزهای ماه
  getMonthDays(year: number, month: number): number {
    if (month <= 6) return 31;
    if (month <= 11) return 30;
    // ماه اسفند
    try {
      const lastDayOfYear = jalaali.toGregorian(year, 12, 30);
      const isLeap = lastDayOfYear.gd === 30 && lastDayOfYear.gm === 3;
      return isLeap ? 30 : 29;
    } catch (error) {
      return 29;
    }
  }
  
// دریافت محدوده تاریخ یک هفته خاص
getWeekDateRange(year: number, week: number): { start: string; end: string; startDate: Date; endDate: Date } {
  const firstDayOfYear = this.parseJalali(`${year}/01/01`);
  const startDate = new Date(firstDayOfYear);
  startDate.setDate(firstDayOfYear.getDate() + (week - 1) * 7);
  const endDate = new Date(startDate);
  endDate.setDate(startDate.getDate() + 6);
  
  return {
    start: this.formatJalali(startDate, false),
    end: this.formatJalali(endDate, false),
    startDate: startDate,
    endDate: endDate
  };
}

// دریافت نام ماه شمسی
getMonthName(month: number): string {
  const months = [
    'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
    'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
  ];
  return months[month - 1];
}

// دریافت عنوان هفته به صورت مفهومی
getWeekTitle(year: number, week: number): string {
  const range = this.getWeekDateRange(year, week);
  const startMonth = this.getMonthName(parseInt(range.start.split('/')[1]));
  const endMonth = this.getMonthName(parseInt(range.end.split('/')[1]));
  const startDay = parseInt(range.start.split('/')[2]);
  const endDay = parseInt(range.end.split('/')[2]);
  
  if (startMonth === endMonth) {
    return `${startMonth} ${startDay} تا ${endDay}`;
  } else {
    return `${startMonth} ${startDay} تا ${endMonth} ${endDay}`;
  }
}
  // متد جدید برای اصلاح تاریخ در loadDailyOrders
  getDateStringForAPI(jalaliDate: string): string {
    const date = this.parseJalali(jalaliDate);
    const year = date.getUTCFullYear();
    const month = (date.getUTCMonth() + 1).toString().padStart(2, '0');
    const day = date.getUTCDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}