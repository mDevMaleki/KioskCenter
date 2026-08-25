import { Component, OnInit, Input, ViewChild, ElementRef } from '@angular/core';
import { PrintService } from '../../services/print.service';
import { CommonModule } from '@angular/common';
import { JalaliDatePipe } from '../../pipes/jalali-date.pipe';
import { FormsModule } from '@angular/forms';
import {
  AccountService,
  Account,
  AccountDto,
  AccountType,
  AccountLedgerItem,
  TrialBalance
} from '../../services/account.service';
import {
  CashAccountService,
  CashAccount,
  CashAccountDto,
  CashAccountType,
  CashAccountLedgerItem
} from '../../services/cash-account.service';
import {
  JournalEntryService,
  JournalEntrySummary,
  JournalEntryDetail,
  JournalEntryRefType,
  JournalEntryLineDto
} from '../../services/journal-entry.service';
import {
  ExpenseService,
  ExpenseItem,
  ExpenseDto
} from '../../services/expense.service';
import {
  FinancialReportService,
  JournalRowDto,
  ProfitLossDto,
  BalanceSheetDto
} from '../../services/financial-report.service';
import {
  ChequeService,
  Cheque,
  ChequeDto,
  ChequeDirection,
  ChequeStatus
} from '../../services/cheque.service';
import { PartyService, Party } from '../../services/party.service';
import {
  FixedAssetService,
  FixedAsset,
  FixedAssetDto,
  DepreciationRecord
} from '../../services/fixed-asset.service';
import {
  PettyCashService,
  PettyCashFund,
  PettyCashFundDto,
  PettyCashTransactionItem
} from '../../services/petty-cash.service';
import {
  BudgetService,
  Budget,
  BudgetDto,
  BudgetVsActual
} from '../../services/budget.service';
import { FiscalYearService, FiscalYear, FiscalYearDto } from '../../services/fiscal-year.service';
import { TaxSettingService, TaxSetting } from '../../services/tax-setting.service';
import { MoadianService, MoadianSettings, MoadianSettingsDto } from '../../services/moadian.service';

type Section = 'accounts' | 'cashAccounts' | 'journalEntries' | 'expenses' | 'trialBalance'
  | 'journalBook' | 'profitLoss' | 'balanceSheet' | 'cheques' | 'fixedAssets' | 'pettyCash' | 'budget'
  | 'fiscalYear' | 'vatReport' | 'moadian';

interface SectionMenuItem {
  key: Section;
  label: string;
  icon: string;
}

interface SectionMenuGroup {
  key: string;
  label: string;
  icon: string;
  items: SectionMenuItem[];
}

@Component({
  selector: 'app-accounting-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, JalaliDatePipe],
  templateUrl: './accounting-admin.component.html',
  styleUrls: ['./accounting-admin.component.css']
})
export class AccountingAdminComponent implements OnInit {

  activeSection: Section = 'accounts';

  @Input() set section(value: string | null) {
    if (value && this.isValidSection(value)) {
      this.setSection(value as Section);
    }
  }

  private isValidSection(value: string): boolean {
    return this.menuGroups.some(g => g.items.some(i => i.key === value));
  }

  menuGroups: SectionMenuGroup[] = [
    {
      key: 'core', label: 'حساب‌ها و اسناد', icon: '📒',
      items: [
        { key: 'accounts', label: 'کدینگ حساب‌ها', icon: '🧾' },
        { key: 'cashAccounts', label: 'صندوق و بانک', icon: '🏦' },
        { key: 'journalEntries', label: 'اسناد حسابداری', icon: '📝' },
        { key: 'journalBook', label: 'دفتر روزنامه', icon: '📔' }
      ]
    },
    {
      key: 'reports', label: 'گزارش‌های مالی', icon: '📊',
      items: [
        { key: 'trialBalance', label: 'تراز آزمایشی', icon: '⚖️' },
        { key: 'profitLoss', label: 'سود و زیان', icon: '📈' },
        { key: 'balanceSheet', label: 'ترازنامه', icon: '📋' },
        { key: 'vatReport', label: 'گزارش ارزش افزوده', icon: '🧮' }
      ]
    },
    {
      key: 'treasury', label: 'خزانه‌داری', icon: '💰',
      items: [
        { key: 'cheques', label: 'مدیریت چک', icon: '🧷' },
        { key: 'pettyCash', label: 'تنخواه‌گردان', icon: '👛' },
        { key: 'expenses', label: 'هزینه‌ها', icon: '🧯' }
      ]
    },
    {
      key: 'assets', label: 'دارایی و بودجه', icon: '🏗️',
      items: [
        { key: 'fixedAssets', label: 'دارایی ثابت', icon: '🏭' },
        { key: 'budget', label: 'بودجه', icon: '🎯' }
      ]
    },
    {
      key: 'settings', label: 'تنظیمات و مالیات', icon: '⚙️',
      items: [
        { key: 'fiscalYear', label: 'سال مالی و مالیات', icon: '🗓️' },
        { key: 'moadian', label: 'سامانه مودیان', icon: '🏛️' }
      ]
    }
  ];

  openGroupKey: string = 'core';

  toggleGroup(groupKey: string): void {
    this.openGroupKey = this.openGroupKey === groupKey ? '' : groupKey;
  }

  isGroupOpen(groupKey: string): boolean {
    return this.openGroupKey === groupKey;
  }

  AccountType = AccountType;
  CashAccountType = CashAccountType;
  JournalEntryRefType = JournalEntryRefType;

  // ---------- کدینگ حساب‌ها ----------
  accounts: Account[] = [];
  accountForm: AccountDto = this.emptyAccountForm();
  editingAccountId: number | null = null;
  accountError = '';

  selectedAccountId: number | null = null;
  accountLedger: AccountLedgerItem[] = [];
  accountLedgerBalance = 0;

  // ---------- صندوق و بانک ----------
  cashAccounts: CashAccount[] = [];
  cashAccountForm: CashAccountDto = this.emptyCashAccountForm();
  editingCashAccountId: number | null = null;
  cashAccountError = '';

  selectedCashAccountId: number | null = null;
  cashAccountLedger: CashAccountLedgerItem[] = [];
  cashAccountLedgerBalance = 0;

  // ---------- اسناد حسابداری ----------
  journalEntries: JournalEntrySummary[] = [];
  journalEntryDetail: JournalEntryDetail | null = null;
  journalForm = { entryDate: '', description: '' };
  journalLines: JournalEntryLineDto[] = [];
  newLine: JournalEntryLineDto = this.emptyLine();
  journalError = '';

  // ---------- هزینه‌ها ----------
  expenses: ExpenseItem[] = [];
  expensesTotal = 0;
  expenseForm: ExpenseDto = this.emptyExpenseForm();
  expenseError = '';

  // ---------- تراز آزمایشی ----------
  trialBalance: TrialBalance | null = null;

  // ---------- گزارش‌های مالی پیشرفته ----------
  reportFrom = '';
  reportTo = '';
  journalBook: JournalRowDto[] = [];
  profitLoss: ProfitLossDto | null = null;
  balanceSheetAsOf = '';
  balanceSheet: BalanceSheetDto | null = null;

  // ---------- مدیریت چک ----------
  ChequeDirection = ChequeDirection;
  ChequeStatus = ChequeStatus;
  cheques: Cheque[] = [];
  chequeFilterDirection: ChequeDirection | '' = '';
  chequeFilterStatus: ChequeStatus | '' = '';
  parties: Party[] = [];
  chequeForm: ChequeDto = this.emptyChequeForm();
  chequeError = '';
  chequeActionCashAccountId: { [chequeId: number]: number } = {};

  // ---------- دارایی ثابت ----------
  fixedAssets: FixedAsset[] = [];
  fixedAssetForm: FixedAssetDto = this.emptyFixedAssetForm();
  fixedAssetError = '';
  fixedAssetHistory: { [id: number]: DepreciationRecord[] } = {};
  depreciationPeriodDate = new Date().toISOString().substring(0, 10);

  // ---------- تنخواه‌گردان ----------
  pettyCashFunds: PettyCashFund[] = [];
  pettyCashFundForm: PettyCashFundDto = { name: '', custodian: '', sourceCashAccountId: 0 };
  pettyCashError = '';
  pettyCashTransactions: { [fundId: number]: PettyCashTransactionItem[] } = {};
  pettyCashReplenishAmount: { [fundId: number]: number } = {};
  pettyCashSpendAmount: { [fundId: number]: number } = {};
  pettyCashSpendAccountId: { [fundId: number]: number } = {};
  pettyCashSpendDescription: { [fundId: number]: string } = {};

  // ---------- بودجه ----------
  budgets: Budget[] = [];
  budgetForm: BudgetDto = this.emptyBudgetForm();
  budgetError = '';
  budgetVsActual: BudgetVsActual[] = [];
  budgetReportFrom = '';
  budgetReportTo = '';

  // ---------- سال مالی ----------
  fiscalYears: FiscalYear[] = [];
  fiscalYearForm: FiscalYearDto = { name: '', startDate: '', endDate: '' };
  fiscalYearError = '';

  // ---------- تنظیمات مالیات بر ارزش افزوده ----------
  taxSetting: TaxSetting | null = null;
  taxSettingError = '';

  // ---------- گزارش مالیات بر ارزش افزوده ----------
  vatReportFrom = '';
  vatReportTo = '';
  vatReport: { inputVat: number; outputVat: number; netVat: number } | null = null;

  // ---------- اتصال به سامانه مودیان ----------
  moadianSettings: MoadianSettings | null = null;
  moadianForm: MoadianSettingsDto = { isEnabled: false, memoryId: '', sellerEconomicCode: '', apiUrl: 'https://tp.tax.gov.ir/requestsmanager', privateKeyPem: '', certificatePem: '' };
  moadianError = '';
  moadianSuccessMsg = '';

  // ---------- جستجو ----------
  searchAccount = '';
  searchCashAccount = '';
  searchJournal = '';
  searchExpense = '';

  get filteredAccounts(): Account[] {
    const q = this.searchAccount.trim().toLowerCase();
    return q ? this.accounts.filter(a =>
      a.code.toLowerCase().includes(q) ||
      a.name.toLowerCase().includes(q)
    ) : this.accounts;
  }

  get filteredCashAccounts(): CashAccount[] {
    const q = this.searchCashAccount.trim().toLowerCase();
    return q ? this.cashAccounts.filter(c => c.name.toLowerCase().includes(q)) : this.cashAccounts;
  }

  get filteredJournalEntries(): JournalEntrySummary[] {
    const q = this.searchJournal.trim().toLowerCase();
    return q ? this.journalEntries.filter(e =>
      (e.description || '').toLowerCase().includes(q) ||
      this.refTypeLabel(e.refType).toLowerCase().includes(q) ||
      String(e.number).includes(q)
    ) : this.journalEntries;
  }

  get filteredExpenses(): ExpenseItem[] {
    const q = this.searchExpense.trim().toLowerCase();
    return q ? this.expenses.filter(e =>
      (e.description || '').toLowerCase().includes(q) ||
      (e.accountName || '').toLowerCase().includes(q) ||
      (e.cashAccountName || '').toLowerCase().includes(q)
    ) : this.expenses;
  }

  constructor(
    private accountService: AccountService,
    private cashAccountService: CashAccountService,
    private journalEntryService: JournalEntryService,
    private expenseService: ExpenseService,
    private financialReportService: FinancialReportService,
    private chequeService: ChequeService,
    private partyService: PartyService,
    private fixedAssetService: FixedAssetService,
    private pettyCashService: PettyCashService,
    private budgetService: BudgetService,
    private fiscalYearService: FiscalYearService,
    private taxSettingService: TaxSettingService,
    private moadianService: MoadianService,
    private printService: PrintService
  ) {}

  @ViewChild('printArea') printAreaRef?: ElementRef<HTMLElement>;
  @ViewChild('printArea2') printAreaRef2?: ElementRef<HTMLElement>;

  printSection(title: string): void {
    if (this.printAreaRef) this.printService.print(title, this.printAreaRef.nativeElement);
  }

  printSection2(title: string): void {
    if (this.printAreaRef2) this.printService.print(title, this.printAreaRef2.nativeElement);
  }

  ngOnInit(): void {
    this.loadAccounts();
    this.loadCashAccounts();
    this.loadJournalEntries();
  }

  setSection(section: Section): void {
    this.activeSection = section;
    const group = this.menuGroups.find(g => g.items.some(i => i.key === section));
    if (group) this.openGroupKey = group.key;
    if (section === 'trialBalance') this.loadTrialBalance();
    if (section === 'expenses') this.loadExpenses();
    if (section === 'journalBook') this.loadJournalBook();
    if (section === 'profitLoss') this.loadProfitLoss();
    if (section === 'balanceSheet') this.loadBalanceSheet();
    if (section === 'cheques') { this.loadCheques(); this.loadParties(); }
    if (section === 'fixedAssets') this.loadFixedAssets();
    if (section === 'pettyCash') this.loadPettyCashFunds();
    if (section === 'budget') this.loadBudgets();
    if (section === 'fiscalYear') { this.loadFiscalYears(); this.loadTaxSetting(); }
    if (section === 'vatReport') this.loadVatReport();
    if (section === 'moadian') this.loadMoadianSettings();
  }

  // ===================== اتصال به سامانه مودیان =====================
  loadMoadianSettings(): void {
    this.moadianService.getSettings().subscribe({
      next: (res) => {
        this.moadianSettings = res;
        this.moadianForm = {
          isEnabled: res.isEnabled,
          memoryId: res.memoryId || '',
          sellerEconomicCode: res.sellerEconomicCode || '',
          apiUrl: res.apiUrl || 'https://tp.tax.gov.ir/requestsmanager',
          privateKeyPem: '',
          certificatePem: ''
        };
      },
      error: (err) => console.error('خطا در دریافت تنظیمات مودیان', err)
    });
  }

  saveMoadianSettings(): void {
    this.moadianError = '';
    this.moadianSuccessMsg = '';

    if (this.moadianForm.isEnabled && (!this.moadianForm.memoryId || !this.moadianForm.sellerEconomicCode)) {
      this.moadianError = 'برای فعال‌سازی، شناسه حافظه مالیاتی و شماره اقتصادی فروشنده الزامی است';
      return;
    }

    this.moadianService.updateSettings(this.moadianForm).subscribe({
      next: () => { this.moadianSuccessMsg = 'تنظیمات با موفقیت ذخیره شد'; this.loadMoadianSettings(); },
      error: (err) => this.moadianError = err?.error?.message || 'خطا در ذخیره تنظیمات'
    });
  }

  // ===================== سال مالی =====================
  loadFiscalYears(): void {
    this.fiscalYearService.getAll().subscribe({
      next: (res) => this.fiscalYears = res,
      error: (err) => console.error('خطا در دریافت سال‌های مالی', err)
    });
  }

  saveFiscalYear(): void {
    this.fiscalYearError = '';
    if (!this.fiscalYearForm.name || !this.fiscalYearForm.startDate || !this.fiscalYearForm.endDate) {
      this.fiscalYearError = 'نام و بازه زمانی الزامی است';
      return;
    }
    this.fiscalYearService.create(this.fiscalYearForm).subscribe({
      next: () => { this.fiscalYearForm = { name: '', startDate: '', endDate: '' }; this.loadFiscalYears(); },
      error: (err) => this.fiscalYearError = err?.error?.message || 'خطا در ثبت سال مالی'
    });
  }

  closeFiscalYear(fy: FiscalYear): void {
    if (!confirm(`سال مالی «${fy.name}» بسته شود؟ پس از این کار امکان ثبت سند جدید در این بازه وجود نخواهد داشت.`)) return;
    this.fiscalYearService.close(fy.id).subscribe({
      next: () => this.loadFiscalYears(),
      error: (err) => alert(err?.error?.message || 'خطا در بستن سال مالی')
    });
  }

  reopenFiscalYear(fy: FiscalYear): void {
    if (!confirm(`سال مالی «${fy.name}» بازگشایی شود؟`)) return;
    this.fiscalYearService.reopen(fy.id).subscribe({
      next: () => this.loadFiscalYears(),
      error: (err) => alert(err?.error?.message || 'خطا در بازگشایی سال مالی')
    });
  }

  deleteFiscalYear(fy: FiscalYear): void {
    if (!confirm(`سال مالی «${fy.name}» حذف شود؟`)) return;
    this.fiscalYearService.delete(fy.id).subscribe({
      next: () => this.loadFiscalYears(),
      error: (err) => alert(err?.error?.message || 'خطا در حذف سال مالی')
    });
  }

  // ===================== تنظیمات مالیات =====================
  loadTaxSetting(): void {
    this.taxSettingService.get().subscribe({
      next: (res) => this.taxSetting = res,
      error: (err) => console.error('خطا در دریافت تنظیمات مالیات', err)
    });
  }

  saveTaxSetting(): void {
    this.taxSettingError = '';
    if (!this.taxSetting) return;
    this.taxSettingService.update(this.taxSetting.vatRate, this.taxSetting.isEnabled).subscribe({
      next: (res) => this.taxSetting = res,
      error: (err) => this.taxSettingError = err?.error?.message || 'خطا در ذخیره تنظیمات مالیات'
    });
  }

  // ===================== گزارش مالیات بر ارزش افزوده =====================
  loadVatReport(): void {
    this.financialReportService.getVatReport(this.vatReportFrom || undefined, this.vatReportTo || undefined).subscribe({
      next: (res: any) => this.vatReport = res,
      error: (err) => console.error('خطا در دریافت گزارش مالیات', err)
    });
  }

  // ===================== دارایی ثابت =====================
  emptyFixedAssetForm(): FixedAssetDto {
    return {
      name: '', purchaseDate: new Date().toISOString().substring(0, 10),
      purchaseValue: 0, salvageValue: 0, usefulLifeMonths: 36,
      assetAccountId: 0, depreciationExpenseAccountId: 0, accumulatedDepreciationAccountId: 0
    };
  }

  loadFixedAssets(): void {
    this.fixedAssetService.getAll().subscribe({
      next: (res) => this.fixedAssets = res,
      error: (err) => console.error('خطا در دریافت دارایی‌های ثابت', err)
    });
  }

  saveFixedAsset(): void {
    this.fixedAssetError = '';
    if (!this.fixedAssetForm.name || !this.fixedAssetForm.purchaseValue || !this.fixedAssetForm.usefulLifeMonths
      || !this.fixedAssetForm.assetAccountId || !this.fixedAssetForm.depreciationExpenseAccountId || !this.fixedAssetForm.accumulatedDepreciationAccountId) {
      this.fixedAssetError = 'تمام فیلدها الزامی است';
      return;
    }
    this.fixedAssetService.create(this.fixedAssetForm).subscribe({
      next: () => { this.fixedAssetForm = this.emptyFixedAssetForm(); this.loadFixedAssets(); },
      error: (err) => this.fixedAssetError = err?.error?.message || 'خطا در ثبت دارایی'
    });
  }

  deleteFixedAsset(id: number): void {
    if (!confirm('این دارایی حذف شود؟')) return;
    this.fixedAssetService.delete(id).subscribe({
      next: () => this.loadFixedAssets(),
      error: (err) => alert(err?.error?.message || 'خطا در حذف دارایی')
    });
  }

  runDepreciation(asset: FixedAsset): void {
    this.fixedAssetService.runDepreciation(asset.id, this.depreciationPeriodDate).subscribe({
      next: () => this.loadFixedAssets(),
      error: (err) => alert(err?.error?.message || 'خطا در ثبت استهلاک')
    });
  }

  runDepreciationAll(): void {
    if (!confirm('استهلاک همه دارایی‌های فعال برای این دوره ثبت شود؟')) return;
    this.fixedAssetService.runDepreciationAll(this.depreciationPeriodDate).subscribe({
      next: (res: any) => { alert(`استهلاک ${res.count} دارایی ثبت شد`); this.loadFixedAssets(); },
      error: (err) => alert(err?.error?.message || 'خطا در ثبت استهلاک گروهی')
    });
  }

  toggleFixedAssetHistory(asset: FixedAsset): void {
    if (this.fixedAssetHistory[asset.id]) {
      delete this.fixedAssetHistory[asset.id];
      return;
    }
    this.fixedAssetService.getHistory(asset.id).subscribe({
      next: (res) => this.fixedAssetHistory[asset.id] = res,
      error: (err) => console.error('خطا در دریافت تاریخچه استهلاک', err)
    });
  }

  // ===================== تنخواه‌گردان =====================
  loadPettyCashFunds(): void {
    this.pettyCashService.getFunds().subscribe({
      next: (res) => this.pettyCashFunds = res,
      error: (err) => console.error('خطا در دریافت تنخواه‌گردان‌ها', err)
    });
  }

  savePettyCashFund(): void {
    this.pettyCashError = '';
    if (!this.pettyCashFundForm.name || !this.pettyCashFundForm.sourceCashAccountId) {
      this.pettyCashError = 'نام و صندوق/بانک منبع الزامی است';
      return;
    }
    this.pettyCashService.createFund(this.pettyCashFundForm).subscribe({
      next: () => { this.pettyCashFundForm = { name: '', custodian: '', sourceCashAccountId: 0 }; this.loadPettyCashFunds(); },
      error: (err) => this.pettyCashError = err?.error?.message || 'خطا در ایجاد تنخواه'
    });
  }

  replenishPettyCash(fund: PettyCashFund): void {
    const amount = this.pettyCashReplenishAmount[fund.id];
    if (!amount) { alert('مبلغ را وارد کنید'); return; }
    this.pettyCashService.replenish(fund.id, amount).subscribe({
      next: () => { this.pettyCashReplenishAmount[fund.id] = 0; this.loadPettyCashFunds(); },
      error: (err) => alert(err?.error?.message || 'خطا در شارژ تنخواه')
    });
  }

  spendPettyCash(fund: PettyCashFund): void {
    const amount = this.pettyCashSpendAmount[fund.id];
    const accountId = this.pettyCashSpendAccountId[fund.id];
    if (!amount || !accountId) { alert('مبلغ و حساب هزینه را انتخاب کنید'); return; }
    this.pettyCashService.spend(fund.id, amount, accountId, this.pettyCashSpendDescription[fund.id]).subscribe({
      next: () => {
        this.pettyCashSpendAmount[fund.id] = 0;
        this.pettyCashSpendDescription[fund.id] = '';
        this.loadPettyCashFunds();
      },
      error: (err) => alert(err?.error?.message || 'خطا در ثبت مصرف تنخواه')
    });
  }

  togglePettyCashTransactions(fund: PettyCashFund): void {
    if (this.pettyCashTransactions[fund.id]) {
      delete this.pettyCashTransactions[fund.id];
      return;
    }
    this.pettyCashService.getTransactions(fund.id).subscribe({
      next: (res) => this.pettyCashTransactions[fund.id] = res,
      error: (err) => console.error('خطا در دریافت تراکنش‌های تنخواه', err)
    });
  }

  // ===================== بودجه =====================
  emptyBudgetForm(): BudgetDto {
    return { accountId: 0, periodStart: '', periodEnd: '', budgetedAmount: 0, note: '' };
  }

  loadBudgets(): void {
    this.budgetService.getAll().subscribe({
      next: (res) => this.budgets = res,
      error: (err) => console.error('خطا در دریافت بودجه‌ها', err)
    });
  }

  saveBudget(): void {
    this.budgetError = '';
    if (!this.budgetForm.accountId || !this.budgetForm.budgetedAmount || !this.budgetForm.periodStart || !this.budgetForm.periodEnd) {
      this.budgetError = 'تمام فیلدها الزامی است';
      return;
    }
    this.budgetService.create(this.budgetForm).subscribe({
      next: () => { this.budgetForm = this.emptyBudgetForm(); this.loadBudgets(); },
      error: (err) => this.budgetError = err?.error?.message || 'خطا در ثبت بودجه'
    });
  }

  deleteBudget(id: number): void {
    if (!confirm('این بودجه حذف شود؟')) return;
    this.budgetService.delete(id).subscribe({
      next: () => this.loadBudgets(),
      error: (err) => alert(err?.error?.message || 'خطا در حذف بودجه')
    });
  }

  loadBudgetVsActual(): void {
    if (!this.budgetReportFrom || !this.budgetReportTo) { alert('بازه زمانی را انتخاب کنید'); return; }
    this.budgetService.getVsActual(this.budgetReportFrom, this.budgetReportTo).subscribe({
      next: (res) => this.budgetVsActual = res,
      error: (err) => console.error('خطا در دریافت بودجه در مقابل واقعی', err)
    });
  }

  // ===================== مدیریت چک =====================
  emptyChequeForm(): ChequeDto {
    return {
      number: '',
      bankName: '',
      issueDate: new Date().toISOString().substring(0, 10),
      dueDate: '',
      amount: 0,
      direction: ChequeDirection.Received,
      partyId: 0,
      description: ''
    };
  }

  loadParties(): void {
    this.partyService.getAll().subscribe({
      next: (res) => this.parties = res,
      error: (err) => console.error('خطا در دریافت طرف‌های حساب', err)
    });
  }

  loadCheques(): void {
    this.chequeService.getAll(
      this.chequeFilterDirection || undefined,
      this.chequeFilterStatus || undefined
    ).subscribe({
      next: (res) => this.cheques = res,
      error: (err) => console.error('خطا در دریافت چک‌ها', err)
    });
  }

  saveCheque(): void {
    this.chequeError = '';
    if (!this.chequeForm.number || !this.chequeForm.amount || !this.chequeForm.partyId || !this.chequeForm.dueDate) {
      this.chequeError = 'شماره چک، مبلغ، طرف حساب و تاریخ سررسید الزامی است';
      return;
    }

    this.chequeService.create(this.chequeForm).subscribe({
      next: () => { this.chequeForm = this.emptyChequeForm(); this.loadCheques(); },
      error: (err) => this.chequeError = err?.error?.message || 'خطا در ثبت چک'
    });
  }

  chequeStatusLabel(status: ChequeStatus): string {
    switch (status) {
      case ChequeStatus.InHand: return 'نزد ما';
      case ChequeStatus.Deposited: return 'سپرده به بانک';
      case ChequeStatus.Cleared: return 'وصول‌شده';
      case ChequeStatus.Bounced: return 'برگشتی';
      case ChequeStatus.Returned: return 'مرجوع‌شده';
      default: return '';
    }
  }

  chequeDirectionLabel(direction: ChequeDirection): string {
    return direction === ChequeDirection.Received ? 'دریافتی' : 'پرداختی';
  }

  depositCheque(cheque: Cheque): void {
    const cashAccountId = this.chequeActionCashAccountId[cheque.id];
    if (!cashAccountId) { alert('ابتدا صندوق/بانک را انتخاب کنید'); return; }
    this.chequeService.deposit(cheque.id, cashAccountId).subscribe({
      next: () => this.loadCheques(),
      error: (err) => alert(err?.error?.message || 'خطا در سپردن چک')
    });
  }

  clearCheque(cheque: Cheque): void {
    const cashAccountId = this.chequeActionCashAccountId[cheque.id] || cheque.cashAccountId || 0;
    if (!cashAccountId) { alert('ابتدا صندوق/بانک را انتخاب کنید'); return; }
    this.chequeService.clear(cheque.id, cashAccountId).subscribe({
      next: () => this.loadCheques(),
      error: (err) => alert(err?.error?.message || 'خطا در وصول چک')
    });
  }

  bounceCheque(cheque: Cheque): void {
    if (!confirm('این چک به‌عنوان برگشتی ثبت شود؟')) return;
    this.chequeService.bounce(cheque.id).subscribe({
      next: () => this.loadCheques(),
      error: (err) => alert(err?.error?.message || 'خطا در ثبت برگشت چک')
    });
  }

  returnCheque(cheque: Cheque): void {
    if (!confirm('این چک مرجوع شود؟ سند اصلاحی ثبت خواهد شد.')) return;
    this.chequeService.returnCheque(cheque.id).subscribe({
      next: () => this.loadCheques(),
      error: (err) => alert(err?.error?.message || 'خطا در مرجوع کردن چک')
    });
  }

  // ===================== گزارش‌های مالی پیشرفته =====================
  loadJournalBook(): void {
    this.financialReportService.getJournal(this.reportFrom || undefined, this.reportTo || undefined).subscribe({
      next: (res) => this.journalBook = res,
      error: (err) => console.error('خطا در دریافت دفتر روزنامه', err)
    });
  }

  loadProfitLoss(): void {
    this.financialReportService.getProfitLoss(this.reportFrom || undefined, this.reportTo || undefined).subscribe({
      next: (res) => this.profitLoss = res,
      error: (err) => console.error('خطا در دریافت سود و زیان', err)
    });
  }

  loadBalanceSheet(): void {
    this.financialReportService.getBalanceSheet(this.balanceSheetAsOf || undefined).subscribe({
      next: (res) => this.balanceSheet = res,
      error: (err) => console.error('خطا در دریافت ترازنامه', err)
    });
  }

  // ===================== کدینگ حساب‌ها =====================
  emptyAccountForm(): AccountDto {
    return { code: '', name: '', type: AccountType.Asset, parentId: null, isGroup: false, isActive: true };
  }

  loadAccounts(): void {
    this.accountService.getAll().subscribe({
      next: (res) => this.accounts = res,
      error: (err) => console.error('خطا در دریافت کدینگ حساب‌ها', err)
    });
  }

  get groupAccounts(): Account[] {
    return this.accounts.filter(a => a.isGroup);
  }

  get nonGroupAccounts(): Account[] {
    return this.accounts.filter(a => !a.isGroup);
  }

  accountTypeLabel(type: AccountType): string {
    switch (type) {
      case AccountType.Asset: return 'دارایی';
      case AccountType.Liability: return 'بدهی';
      case AccountType.Equity: return 'حقوق صاحبان سرمایه';
      case AccountType.Revenue: return 'درآمد';
      case AccountType.Expense: return 'هزینه';
      default: return '';
    }
  }

  accountName(id: number | null | undefined): string {
    if (!id) return '-';
    return this.accounts.find(a => a.id === id)?.name || '-';
  }

  saveAccount(): void {
    this.accountError = '';

    if (!this.accountForm.code || !this.accountForm.name) {
      this.accountError = 'کد و نام حساب الزامی است';
      return;
    }

    if (this.editingAccountId) {
      this.accountService.update(this.editingAccountId, this.accountForm).subscribe({
        next: () => { this.cancelEditAccount(); this.loadAccounts(); },
        error: (err) => this.accountError = err?.error?.message || 'خطا در ویرایش حساب'
      });
    } else {
      this.accountService.create(this.accountForm).subscribe({
        next: () => { this.cancelEditAccount(); this.loadAccounts(); },
        error: (err) => this.accountError = err?.error?.message || 'خطا در افزودن حساب'
      });
    }
  }

  editAccount(account: Account): void {
    this.editingAccountId = account.id;
    this.accountForm = {
      code: account.code,
      name: account.name,
      type: account.type,
      parentId: account.parentId,
      isGroup: account.isGroup,
      isActive: account.isActive
    };
    this.accountError = '';
  }

  cancelEditAccount(): void {
    this.editingAccountId = null;
    this.accountForm = this.emptyAccountForm();
    this.accountError = '';
  }

  deleteAccount(id: number): void {
    if (!confirm('این حساب حذف شود؟')) return;

    this.accountService.delete(id).subscribe({
      next: () => {
        if (this.selectedAccountId === id) this.selectedAccountId = null;
        this.loadAccounts();
      },
      error: (err) => alert(err?.error?.message || 'خطا در حذف حساب')
    });
  }

  selectAccount(id: number): void {
    this.selectedAccountId = id;
    this.accountService.getLedger(id).subscribe({
      next: (res) => {
        this.accountLedger = res.items;
        this.accountLedgerBalance = res.balance;
      },
      error: (err) => console.error('خطا در دریافت دفتر حساب', err)
    });
  }

  get selectedAccountName(): string {
    return this.accounts.find(a => a.id === this.selectedAccountId)?.name || '';
  }

  // ===================== صندوق و بانک =====================
  emptyCashAccountForm(): CashAccountDto {
    return { name: '', type: CashAccountType.Cash, accountNumber: '', accountId: 0, isActive: true };
  }

  loadCashAccounts(): void {
    this.cashAccountService.getAll().subscribe({
      next: (res) => this.cashAccounts = res,
      error: (err) => console.error('خطا در دریافت صندوق و بانک', err)
    });
  }

  cashAccountTypeLabel(type: CashAccountType): string {
    return type === CashAccountType.Cash ? 'صندوق' : 'بانک';
  }

  saveCashAccount(): void {
    this.cashAccountError = '';

    if (!this.cashAccountForm.name || !this.cashAccountForm.accountId) {
      this.cashAccountError = 'نام و حساب معین مرتبط الزامی است';
      return;
    }

    if (this.editingCashAccountId) {
      this.cashAccountService.update(this.editingCashAccountId, this.cashAccountForm).subscribe({
        next: () => { this.cancelEditCashAccount(); this.loadCashAccounts(); },
        error: (err) => this.cashAccountError = err?.error?.message || 'خطا در ویرایش صندوق/بانک'
      });
    } else {
      this.cashAccountService.create(this.cashAccountForm).subscribe({
        next: () => { this.cancelEditCashAccount(); this.loadCashAccounts(); },
        error: (err) => this.cashAccountError = err?.error?.message || 'خطا در افزودن صندوق/بانک'
      });
    }
  }

  editCashAccount(cashAccount: CashAccount): void {
    this.editingCashAccountId = cashAccount.id;
    this.cashAccountForm = {
      name: cashAccount.name,
      type: cashAccount.type,
      accountNumber: cashAccount.accountNumber,
      accountId: cashAccount.accountId,
      isActive: cashAccount.isActive
    };
    this.cashAccountError = '';
  }

  cancelEditCashAccount(): void {
    this.editingCashAccountId = null;
    this.cashAccountForm = this.emptyCashAccountForm();
    this.cashAccountError = '';
  }

  deleteCashAccount(id: number): void {
    if (!confirm('این صندوق/بانک حذف شود؟')) return;

    this.cashAccountService.delete(id).subscribe({
      next: () => {
        if (this.selectedCashAccountId === id) this.selectedCashAccountId = null;
        this.loadCashAccounts();
      },
      error: (err) => alert(err?.error?.message || 'خطا در حذف صندوق/بانک')
    });
  }

  selectCashAccount(id: number): void {
    this.selectedCashAccountId = id;
    this.cashAccountService.getLedger(id).subscribe({
      next: (res) => {
        this.cashAccountLedger = res.items;
        this.cashAccountLedgerBalance = res.balance;
      },
      error: (err) => console.error('خطا در دریافت گردش صندوق/بانک', err)
    });
  }

  get selectedCashAccountName(): string {
    return this.cashAccounts.find(c => c.id === this.selectedCashAccountId)?.name || '';
  }

  // ===================== اسناد حسابداری =====================
  emptyLine(): JournalEntryLineDto {
    return { accountId: 0, debit: 0, credit: 0, description: '', partyId: null, cashAccountId: null };
  }

  loadJournalEntries(): void {
    this.journalEntryService.getAll().subscribe({
      next: (res) => this.journalEntries = res.items,
      error: (err) => console.error('خطا در دریافت اسناد حسابداری', err)
    });
  }

  refTypeLabel(type: JournalEntryRefType): string {
    switch (type) {
      case JournalEntryRefType.Manual: return 'سند دستی';
      case JournalEntryRefType.PurchaseInvoice: return 'فاکتور خرید';
      case JournalEntryRefType.SaleInvoice: return 'فاکتور فروش';
      case JournalEntryRefType.Payment: return 'پرداخت';
      case JournalEntryRefType.Receipt: return 'دریافت';
      case JournalEntryRefType.Expense: return 'هزینه';
      default: return '';
    }
  }

  addJournalLine(): void {
    this.journalError = '';

    if (!this.newLine.accountId || (this.newLine.debit <= 0 && this.newLine.credit <= 0)) {
      this.journalError = 'انتخاب حساب و مقدار بدهکار یا بستانکار الزامی است';
      return;
    }

    this.journalLines.push({ ...this.newLine });
    this.newLine = this.emptyLine();
  }

  removeJournalLine(index: number): void {
    this.journalLines.splice(index, 1);
  }

  get journalTotalDebit(): number {
    return this.journalLines.reduce((sum, l) => sum + (l.debit || 0), 0);
  }

  get journalTotalCredit(): number {
    return this.journalLines.reduce((sum, l) => sum + (l.credit || 0), 0);
  }

  submitJournalEntry(): void {
    this.journalError = '';

    if (this.journalLines.length < 2) {
      this.journalError = 'سند حسابداری باید حداقل دو ردیف داشته باشد';
      return;
    }

    if (this.journalTotalDebit !== this.journalTotalCredit) {
      this.journalError = 'سند حسابداری باید موازنه باشد (جمع بدهکار = جمع بستانکار)';
      return;
    }

    this.journalEntryService.create({
      entryDate: this.journalForm.entryDate || new Date().toISOString(),
      description: this.journalForm.description,
      lines: this.journalLines
    }).subscribe({
      next: () => {
        this.journalForm = { entryDate: '', description: '' };
        this.journalLines = [];
        this.loadJournalEntries();
        this.loadAccounts();
      },
      error: (err) => this.journalError = err?.error?.message || 'خطا در ثبت سند حسابداری'
    });
  }

  viewJournalEntry(id: number): void {
    this.journalEntryService.getOne(id).subscribe({
      next: (res) => this.journalEntryDetail = res,
      error: (err) => console.error('خطا در دریافت سند حسابداری', err)
    });
  }

  closeJournalEntryDetail(): void {
    this.journalEntryDetail = null;
  }

  deleteJournalEntry(id: number): void {
    if (!confirm('این سند حسابداری حذف شود؟')) return;

    this.journalEntryService.delete(id).subscribe({
      next: () => {
        this.closeJournalEntryDetail();
        this.loadJournalEntries();
        this.loadAccounts();
      },
      error: (err) => alert(err?.error?.message || 'خطا در حذف سند حسابداری')
    });
  }

  // ===================== هزینه‌ها =====================
  emptyExpenseForm(): ExpenseDto {
    return { date: '', description: '', amount: 0, accountId: 0, cashAccountId: 0 };
  }

  get expenseAccounts(): Account[] {
    return this.accounts.filter(a => !a.isGroup && a.type === AccountType.Expense);
  }

  loadExpenses(): void {
    this.expenseService.getAll().subscribe({
      next: (res) => {
        this.expenses = res.items;
        this.expensesTotal = res.totalAmount;
      },
      error: (err) => console.error('خطا در دریافت هزینه‌ها', err)
    });
  }

  submitExpense(): void {
    this.expenseError = '';

    if (!this.expenseForm.amount || this.expenseForm.amount <= 0) {
      this.expenseError = 'مبلغ هزینه باید بزرگ‌تر از صفر باشد';
      return;
    }

    if (!this.expenseForm.accountId) {
      this.expenseError = 'انتخاب حساب هزینه الزامی است';
      return;
    }

    if (!this.expenseForm.cashAccountId) {
      this.expenseError = 'انتخاب صندوق/بانک پرداخت‌کننده الزامی است';
      return;
    }

    this.expenseService.create({
      ...this.expenseForm,
      date: this.expenseForm.date || new Date().toISOString()
    }).subscribe({
      next: () => {
        this.expenseForm = this.emptyExpenseForm();
        this.loadExpenses();
        this.loadAccounts();
        this.loadCashAccounts();
      },
      error: (err) => this.expenseError = err?.error?.message || 'خطا در ثبت هزینه'
    });
  }

  deleteExpense(id: number): void {
    if (!confirm('این هزینه حذف شود؟')) return;

    this.expenseService.delete(id).subscribe({
      next: () => {
        this.loadExpenses();
        this.loadAccounts();
        this.loadCashAccounts();
      },
      error: (err) => alert(err?.error?.message || 'خطا در حذف هزینه')
    });
  }

  // ===================== تراز آزمایشی =====================
  loadTrialBalance(): void {
    this.accountService.getTrialBalance().subscribe({
      next: (res) => this.trialBalance = res,
      error: (err) => console.error('خطا در دریافت تراز آزمایشی', err)
    });
  }
}
