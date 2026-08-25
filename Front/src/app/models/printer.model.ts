export interface PrinterSetting {
  id: number;
  name: string;           // نام نمایشی (آشپزخانه، صندوق، کیوسک)
  printerName: string;    // نام واقعی پرینتر در ویندوز
  printerType: string;    // Receipt, Kitchen, Label
  source?: string;        // Kiosk, Cashier, Both - فقط برای پرینتر فیش معنا دارد
  isActive: boolean;
  priority: number;
  categories: string[];   // برای پرینتر آشپزخانه
  productIds: number[];   // برای پرینتر آشپزخانه
  createdAt: Date;
  updatedAt?: Date;
}