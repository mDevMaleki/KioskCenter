export interface PosDevice {
  id: number;
  name: string;
  type: string; // "Parsian" یا "PardakhtNovin"
  ipAddress: string;
  port: number;
  isActive: boolean;
  isDefault: boolean;
  priority: number;
  createdAt: Date;
  updatedAt?: Date;
}

export interface PosPayRequest {
  amount: number;
  deviceId?: number;
}

export interface PosPaymentResponse {
  success: boolean;
  message: string;
  responseValue?: string;
  amount?: number;
  prn?: string;
  pan?: string;
  terminalId?: string;
  transactionDate?: string;
}

export interface PosConnectionResponse {
  success: boolean;
  message: string;
}