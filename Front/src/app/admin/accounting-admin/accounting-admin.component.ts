import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
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

type Section = 'accounts' | 'cashAccounts' | 'journalEntries' | 'expenses' | 'trialBalance';

@Component({
  selector: 'app-accounting-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './accounting-admin.component.html',
  styleUrls: ['./accounting-admin.component.css']
})
export class AccountingAdminComponent implements OnInit {

  activeSection: Section = 'accounts';

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
    private expenseService: ExpenseService
  ) {}

  ngOnInit(): void {
    this.loadAccounts();
    this.loadCashAccounts();
    this.loadJournalEntries();
  }

  setSection(section: Section): void {
    this.activeSection = section;
    if (section === 'trialBalance') this.loadTrialBalance();
    if (section === 'expenses') this.loadExpenses();
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
