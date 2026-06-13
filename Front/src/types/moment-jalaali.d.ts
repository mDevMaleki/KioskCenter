// src/types/moment-jalaali.d.ts
declare module 'moment-jalaali' {
  import { Moment } from 'moment';
  
  interface MomentJalali extends Moment {
    jYear(): number;
    jMonth(): number;
    jDate(): number;
    jDayOfYear(): number;
    jWeek(): number;
    jWeekYear(): number;
    format(format?: string): string;
    isLeapYear(): boolean;
  }
  
  function moment(date?: any, format?: string, strict?: boolean): MomentJalali;
  function moment(date?: any, format?: string, locale?: string, strict?: boolean): MomentJalali;
  
  export default moment;
}