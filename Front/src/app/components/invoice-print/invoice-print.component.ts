import { Component, Input, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export type InvoicePrintTemplate = 'A4' | 'A5' | 'Thermal80';

export interface InvoicePrintItem {
  name: string;
  unit?: string | null;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

const TEMPLATE_STORAGE_KEY = 'invoicePrintTemplate';

@Component({
  selector: 'app-invoice-print',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './invoice-print.component.html',
  styleUrls: ['./invoice-print.component.css']
})
export class InvoicePrintComponent implements AfterViewInit {
  @ViewChild('printArea') printAreaRef!: ElementRef<HTMLDivElement>;

  constructor(private hostRef: ElementRef<HTMLElement>) {}

  ngAfterViewInit(): void {
    // انتقال این کامپوننت به فرزند مستقیم body تا در حالت چاپ بقیه صفحه قابل مخفی‌سازی باشد
    document.body.appendChild(this.hostRef.nativeElement);
  }

  visible = false;

  invoiceTypeLabel = '';
  invoiceNumber = 0;
  partyLabel = '';
  partyName = '';
  date = '';
  items: InvoicePrintItem[] = [];
  subtotal = 0;
  vatRate = 0;
  vatAmount = 0;
  grandTotal = 0;
  note: string | null = '';

  template: InvoicePrintTemplate = this.loadSavedTemplate();

  templates: { key: InvoicePrintTemplate; label: string }[] = [
    { key: 'A4', label: 'A4 (کامل)' },
    { key: 'A5', label: 'A5 (نیمه)' },
    { key: 'Thermal80', label: 'فیش حرارتی 80mm' }
  ];

  private loadSavedTemplate(): InvoicePrintTemplate {
    const saved = localStorage.getItem(TEMPLATE_STORAGE_KEY);
    if (saved === 'A4' || saved === 'A5' || saved === 'Thermal80') return saved;
    return 'A4';
  }

  onTemplateChange(): void {
    localStorage.setItem(TEMPLATE_STORAGE_KEY, this.template);
    this.applyPageSize();
  }

  private applyPageSize(): void {
    const sizes: Record<InvoicePrintTemplate, string> = {
      A4: '@page { size: A4; margin: 10mm; }',
      A5: '@page { size: A5; margin: 8mm; }',
      Thermal80: '@page { size: 80mm auto; margin: 2mm; }'
    };

    let styleTag = document.getElementById('invoice-print-page-size') as HTMLStyleElement | null;
    if (!styleTag) {
      styleTag = document.createElement('style');
      styleTag.id = 'invoice-print-page-size';
      document.head.appendChild(styleTag);
    }
    styleTag.textContent = sizes[this.template];
  }

  open(options: {
    invoiceTypeLabel: string;
    invoiceNumber: number;
    partyLabel: string;
    partyName: string;
    date: string;
    items: InvoicePrintItem[];
    subtotal: number;
    vatRate: number;
    vatAmount: number;
    grandTotal: number;
    note?: string | null;
  }): void {
    this.invoiceTypeLabel = options.invoiceTypeLabel;
    this.invoiceNumber = options.invoiceNumber;
    this.partyLabel = options.partyLabel;
    this.partyName = options.partyName;
    this.date = options.date;
    this.items = options.items;
    this.subtotal = options.subtotal;
    this.vatRate = options.vatRate;
    this.vatAmount = options.vatAmount;
    this.grandTotal = options.grandTotal;
    this.note = options.note || '';
    this.visible = true;
    this.applyPageSize();
    document.body.classList.add('invoice-printing-active');
  }

  close(): void {
    this.visible = false;
    document.body.classList.remove('invoice-printing-active');
  }

  print(): void {
    window.print();
  }
}
