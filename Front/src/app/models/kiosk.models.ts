export interface Category {
  id: number;
  name: string;
  description?: string;
  icon?: string;
}

export interface Product {
  id: number;
  name: string;
  price: number;
  categoryId: number;
  imageUrl?:string;
  description?:string;
  state?:number;
  secondPrice?:number| 0; 
}
// اضافه کردن به فایل kiosk.models.ts
// اضافه کنید به kiosk.models.ts

export interface OrderTypeSetting {
  id: 'EatIn' | 'TakeAway';
  name: string;
  persianName: string;
  icon: string;
  active: boolean;
  priceFactor: number;
  visibleCategoryIds: string[];
  categoryPriceRules: CategoryPriceRule[];
}

export interface CategoryPriceRule {
  categoryId: string;
  factor: number;
}

export interface ProductWithPrice {
  id: number;
  name: string;
  price: number;
  secondPrice?: number;
  finalPrice: number;
  categoryId: number;
  categoryName: string;
  imageUrl: string;
  description: string;
  state: number;
}
export interface CartItem {
  productId: number;
  productName: string;
  price: number;
  quantity: number;
  totalPrice: number;
}

export interface OrderType {
  id: string;
  name: string;
  icon: string;
}

export interface KioskState {
  currentStep: number;
  selectedCategory: Category | null;
  cart: CartItem[];
  orderType: string;
  customerName: string;
}
export interface CreateOrderItemDto {
  productId: number;
  quantity: number;
  price: number;
}

export interface CreateOrderDto {
  orderType: string;
  customerName: string;
  items: CreateOrderItemDto[];  // از نوع CreateOrderItemDto استفاده می‌کنیم
  totalAmount: number;
  paymentMethod?: string;
  
}

// اضافه کردن اینترفیس‌های جدید برای پرداخت
export interface PosPaymentResponse {
  success: boolean;
  message?: string;
  transactionId?: string;
  amount?: number;
  isSuccess?: boolean;
}

export interface OnlinePaymentQrResponse {
  qrCodeImage: string;
  paymentId: string;
  amount: number;
}