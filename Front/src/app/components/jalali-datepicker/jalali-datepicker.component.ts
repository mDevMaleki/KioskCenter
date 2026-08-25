import { Component, ElementRef, HostListener, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
// @ts-ignore
import * as jalaali from 'jalaali-js';

interface DayCell {
  jd: number;
  isToday: boolean;
  isSelected: boolean;
  inCurrentMonth: boolean;
  gy: number;
  gm: number;
  gd: number;
}

const MONTH_NAMES = [
  'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
  'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
];

const WEEKDAY_NAMES = ['ش', 'ی', 'د', 'س', 'چ', 'پ', 'ج'];

@Component({
  selector: 'app-jalali-datepicker',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './jalali-datepicker.component.html',
  styleUrls: ['./jalali-datepicker.component.css'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => JalaliDatepickerComponent),
      multi: true
    }
  ]
})
export class JalaliDatepickerComponent implements ControlValueAccessor {
  open = false;
  disabled = false;

  // مقدار میلادی ISO (yyyy-MM-dd) - همان چیزی که input[type=date] قبلاً برمی‌گرداند
  private value: string | null = null;

  // متن نمایشی شمسی داخل کادر
  displayText = '';

  // وضعیت تقویم باز شده
  viewYear = 0;
  viewMonth = 1;
  selectedJy = 0;
  selectedJm = 0;
  selectedJd = 0;

  monthNames = MONTH_NAMES;
  weekdayNames = WEEKDAY_NAMES;
  days: DayCell[] = [];

  private onChange: (value: string | null) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(private elRef: ElementRef<HTMLElement>) {
    const now = new Date();
    const today = jalaali.toJalaali(now.getFullYear(), now.getMonth() + 1, now.getDate());
    this.viewYear = today.jy;
    this.viewMonth = today.jm;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.open && !this.elRef.nativeElement.contains(event.target as Node)) {
      this.open = false;
    }
  }

  writeValue(value: string | null): void {
    this.value = value || null;

    if (this.value) {
      const [gy, gm, gd] = this.value.split('-').map(n => parseInt(n, 10));
      if (gy && gm && gd) {
        const j = jalaali.toJalaali(gy, gm, gd);
        this.selectedJy = j.jy;
        this.selectedJm = j.jm;
        this.selectedJd = j.jd;
        this.viewYear = j.jy;
        this.viewMonth = j.jm;
        this.displayText = `${j.jy}/${this.pad(j.jm)}/${this.pad(j.jd)}`;
      }
    } else {
      this.selectedJy = 0;
      this.selectedJm = 0;
      this.selectedJd = 0;
      this.displayText = '';
    }

    this.buildCalendar();
  }

  registerOnChange(fn: (value: string | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  private pad(n: number): string {
    return n.toString().padStart(2, '0');
  }

  toggle(): void {
    if (this.disabled) return;
    this.open = !this.open;
    if (this.open) this.buildCalendar();
  }

  prevMonth(): void {
    this.viewMonth--;
    if (this.viewMonth < 1) {
      this.viewMonth = 12;
      this.viewYear--;
    }
    this.buildCalendar();
  }

  nextMonth(): void {
    this.viewMonth++;
    if (this.viewMonth > 12) {
      this.viewMonth = 1;
      this.viewYear++;
    }
    this.buildCalendar();
  }

  private buildCalendar(): void {
    const now = new Date();
    const today = jalaali.toJalaali(now.getFullYear(), now.getMonth() + 1, now.getDate());
    const monthLength = jalaali.jalaaliMonthLength(this.viewYear, this.viewMonth);

    // محاسبه روز هفته اول ماه (0=شنبه ... 6=جمعه)
    const firstGregorian = jalaali.toGregorian(this.viewYear, this.viewMonth, 1);
    const firstDate = new Date(firstGregorian.gy, firstGregorian.gm - 1, firstGregorian.gd);
    const jsWeekday = firstDate.getDay(); // 0=یکشنبه در جاوااسکریپت
    const offset = (jsWeekday + 1) % 7; // تبدیل به آفست با شروع شنبه

    const cells: DayCell[] = [];
    for (let i = 0; i < offset; i++) {
      cells.push({ jd: 0, isToday: false, isSelected: false, inCurrentMonth: false, gy: 0, gm: 0, gd: 0 });
    }

    for (let d = 1; d <= monthLength; d++) {
      const g = jalaali.toGregorian(this.viewYear, this.viewMonth, d);
      cells.push({
        jd: d,
        isToday: today.jy === this.viewYear && today.jm === this.viewMonth && today.jd === d,
        isSelected: this.selectedJy === this.viewYear && this.selectedJm === this.viewMonth && this.selectedJd === d,
        inCurrentMonth: true,
        gy: g.gy,
        gm: g.gm,
        gd: g.gd
      });
    }

    this.days = cells;
  }

  selectDay(cell: DayCell): void {
    if (!cell.inCurrentMonth) return;

    this.selectedJy = this.viewYear;
    this.selectedJm = this.viewMonth;
    this.selectedJd = cell.jd;

    const iso = `${cell.gy}-${this.pad(cell.gm)}-${this.pad(cell.gd)}`;
    this.value = iso;
    this.displayText = `${this.viewYear}/${this.pad(this.viewMonth)}/${this.pad(cell.jd)}`;

    this.onChange(iso);
    this.onTouched();
    this.open = false;
    this.buildCalendar();
  }

  selectToday(): void {
    const now = new Date();
    const today = jalaali.toJalaali(now.getFullYear(), now.getMonth() + 1, now.getDate());
    this.viewYear = today.jy;
    this.viewMonth = today.jm;
    this.buildCalendar();
    const todayCell = this.days.find(c => c.isToday);
    if (todayCell) this.selectDay(todayCell);
  }

  clear(event: Event): void {
    event.stopPropagation();
    this.value = null;
    this.displayText = '';
    this.selectedJy = 0;
    this.selectedJm = 0;
    this.selectedJd = 0;
    this.onChange(null);
    this.onTouched();
    this.buildCalendar();
  }
}
