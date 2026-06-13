export interface PrinterSetting {
  id: number;
  name: string;           // نام نمایشی (آشپزخانه، صندوق، کیوسک)
  printerName: string;    // نام واقعی پرینتر در ویندوز
  printerType: string;    // Receipt, Kitchen, Label
  isActive: boolean;
  priority: number;
  categories: string[];   // برای پرینتر آشپزخانه
  productIds: number[];   // برای پرینتر آشپزخانه
  createdAt: Date;
  updatedAt?: Date;
}