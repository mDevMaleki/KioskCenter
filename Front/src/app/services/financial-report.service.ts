import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AccountType } from './account.service';

export interface JournalLineDto {
  accountCode: string;
  accountName: string;
  debit: number;
  credit: number;
  description?: string | null;
}

export interface JournalRowDto {
  journalEntryId: number;
  number: number;
  entryDate: string;
  description?: string | null;
  refType: number;
  lines: JournalLineDto[];
}

export interface LedgerLineDto {
  journalEntryId: number;
  number: number;
  entryDate: string;
  description?: string | null;
  debit: number;
  credit: number;
  runningBalance: number;
}

export interface AccountLedgerDto {
  accountId: number;
  accountCode: string;
  accountName: string;
  openingBalance: number;
  closingBalance: number;
  lines: LedgerLineDto[];
}

export interface TrialBalanceRowDto {
  accountId: number;
  accountCode: string;
  accountName: string;
  type: AccountType;
  totalDebit: number;
  totalCredit: number;
  balance: number;
}

export interface ProfitLossDto {
  revenues: TrialBalanceRowDto[];
  expenses: TrialBalanceRowDto[];
  totalRevenue: number;
  totalExpense: number;
  netProfit: number;
}

export interface BalanceSheetDto {
  assets: TrialBalanceRowDto[];
  liabilities: TrialBalanceRowDto[];
  equity: TrialBalanceRowDto[];
  totalAssets: number;
  totalLiabilities: number;
  totalEquity: number;
  netProfitToDate: number;
}

@Injectable({ providedIn: 'root' })
export class FinancialReportService {
  private apiUrl = 'http://localhost:5000/api/financialreport';

  constructor(private http: HttpClient) {}

  private dateParams(from?: string, to?: string): string {
    const params: string[] = [];
    if (from) params.push(`from=${from}`);
    if (to) params.push(`to=${to}`);
    return params.length ? '?' + params.join('&') : '';
  }

  getJournal(from?: string, to?: string): Observable<JournalRowDto[]> {
    return this.http.get<JournalRowDto[]>(`${this.apiUrl}/journal${this.dateParams(from, to)}`);
  }

  getAccountLedger(accountId: number, from?: string, to?: string): Observable<AccountLedgerDto> {
    return this.http.get<AccountLedgerDto>(`${this.apiUrl}/ledger/${accountId}${this.dateParams(from, to)}`);
  }

  getTrialBalance(from?: string, to?: string): Observable<TrialBalanceRowDto[]> {
    return this.http.get<TrialBalanceRowDto[]>(`${this.apiUrl}/trialbalance${this.dateParams(from, to)}`);
  }

  getProfitLoss(from?: string, to?: string): Observable<ProfitLossDto> {
    return this.http.get<ProfitLossDto>(`${this.apiUrl}/profitloss${this.dateParams(from, to)}`);
  }

  getBalanceSheet(asOf?: string): Observable<BalanceSheetDto> {
    return this.http.get<BalanceSheetDto>(`${this.apiUrl}/balancesheet${asOf ? '?asOf=' + asOf : ''}`);
  }

  getVatReport(from?: string, to?: string): Observable<{ inputVat: number; outputVat: number; netVat: number }> {
    return this.http.get<{ inputVat: number; outputVat: number; netVat: number }>(`${this.apiUrl}/vat-report${this.dateParams(from, to)}`);
  }
}
