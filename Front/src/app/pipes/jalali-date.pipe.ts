import { Pipe, PipeTransform } from '@angular/core';
import { JalaliCalendarService } from '../services/jalali-calendar.service';

@Pipe({
  name: 'jalaliDate',
  standalone: true
})
export class JalaliDatePipe implements PipeTransform {
  constructor(private jalaliCalendar: JalaliCalendarService) {}
  
  transform(value: string | Date | null | undefined, withTime: boolean = false): string {
    if (!value) return '-';
    
    try {
      const date = new Date(value);
      if (isNaN(date.getTime())) return '-';
      
      return this.jalaliCalendar.formatJalali(date, withTime);
    } catch (error) {
      console.error('Error converting date to jalali:', error);
      return '-';
    }
  }
}