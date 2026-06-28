import { Injectable } from '@angular/core';

export type PrintTemplate = 'A4' | 'A5' | 'Thermal80';

interface ExtractedTable {
  headers: string[];
  rows: string[][];
}

const TEMPLATE_STORAGE_KEY = 'globalPrintTemplate';

@Injectable({ providedIn: 'root' })
export class PrintService {
  private templates: { key: PrintTemplate; label: string }[] = [
    { key: 'A4', label: 'A4 (گزارش کامل)' },
    { key: 'A5', label: 'A5 (گزارش خلاصه)' },
    { key: 'Thermal80', label: 'فیش حرارتی 80mm' }
  ];

  private loadSavedTemplate(): PrintTemplate {
    const saved = localStorage.getItem(TEMPLATE_STORAGE_KEY);
    if (saved === 'A4' || saved === 'A5' || saved === 'Thermal80') return saved;
    return 'A4';
  }

  private applyPageSize(template: PrintTemplate): void {
    const sizes: Record<PrintTemplate, string> = {
      A4: '@page { size: A4; margin: 12mm; }',
      A5: '@page { size: A5; margin: 8mm; }',
      Thermal80: '@page { size: 80mm auto; margin: 2mm; }'
    };

    let styleTag = document.getElementById('global-print-page-size') as HTMLStyleElement | null;
    if (!styleTag) {
      styleTag = document.createElement('style');
      styleTag.id = 'global-print-page-size';
      document.head.appendChild(styleTag);
    }
    styleTag.textContent = sizes[template];
  }

  // ---------------------------------------------------------------------
  // استخراج داده ساختاریافته از DOM منبع (table استاندارد یا الگوی tx-table)
  // ---------------------------------------------------------------------
  private extractTable(source: HTMLElement): ExtractedTable | null {
    const table = source.tagName === 'TABLE' ? source : source.querySelector('table');
    if (table) {
      const headers = Array.from(table.querySelectorAll('thead th')).map(c => (c.textContent || '').trim());
      const rows = Array.from(table.querySelectorAll('tbody tr')).map(tr =>
        Array.from(tr.querySelectorAll('td')).map(td => (td.textContent || '').trim())
      );
      if (rows.length > 0 || headers.length > 0) return { headers, rows };
    }

    const txTable = source.classList.contains('tx-table') ? source : source.querySelector('.tx-table');
    if (txTable) {
      const allRows = Array.from(txTable.children).filter(c => c.classList.contains('tx-row'));
      const headerRow = allRows.find(r => r.classList.contains('tx-header'));
      const dataRows = allRows.filter(r => !r.classList.contains('tx-header'));
      const headers = headerRow ? Array.from(headerRow.children).map(s => (s.textContent || '').trim()) : [];
      const rows = dataRows.map(r => Array.from(r.children).map(s => (s.textContent || '').trim()));
      if (rows.length > 0 || headers.length > 0) return { headers, rows };
    }

    return null;
  }

  // فهرست کارت‌ها (material-card / user-card / cheque-card و ...) به‌عنوان fallback
  private extractCards(source: HTMLElement): string[][] | null {
    const cardSelectors = '.material-card, .user-card, .cheque-card, .category-card, .product-card, .stock-item, .method-item';
    let cards = Array.from(source.querySelectorAll(cardSelectors));
    if (cards.length === 0) {
      cards = Array.from(source.children).filter(c => (c.textContent || '').trim().length > 0);
    }
    if (cards.length === 0) return null;

    return cards.map(card => {
      const clone = card.cloneNode(true) as HTMLElement;
      clone.querySelectorAll('button, input, select, .buttons, .cheque-actions').forEach(el => el.remove());
      return this.splitIntoFields(clone);
    });
  }

  private splitIntoFields(el: HTMLElement): string[] {
    const lines: string[] = [];
    el.querySelectorAll('.name, .meta span, .info, span').forEach(node => {
      const t = (node.textContent || '').trim();
      if (t) lines.push(t);
    });
    if (lines.length === 0) {
      const t = (el.textContent || '').trim();
      if (t) lines.push(t);
    }
    return Array.from(new Set(lines));
  }

  // ---------------------------------------------------------------------
  // ساخت فرم گزارش بر اساس قالب انتخابی (نه کپی مستقیم استایل صفحه)
  // ---------------------------------------------------------------------
  private buildReportSheet(title: string, source: HTMLElement, template: PrintTemplate): HTMLElement {
    const sheet = document.createElement('div');
    sheet.className = 'report-sheet report-' + template;

    const header = document.createElement('div');
    header.className = 'report-header';

    const heading = document.createElement('div');
    heading.className = 'report-title';
    heading.textContent = title;

    const dateLine = document.createElement('div');
    dateLine.className = 'report-date';
    dateLine.textContent = new Date().toLocaleDateString('fa-IR');

    header.appendChild(heading);
    header.appendChild(dateLine);
    sheet.appendChild(header);

    const tableData = this.extractTable(source);

    if (template === 'Thermal80') {
      // فرم رسیدی: هر ردیف به‌صورت چند خط «برچسب: مقدار» پشت‌سرهم
      const list = document.createElement('div');
      list.className = 'report-receipt-list';

      if (tableData && tableData.rows.length > 0) {
        tableData.rows.forEach(row => {
          const block = document.createElement('div');
          block.className = 'report-receipt-row';
          row.forEach((cell, i) => {
            if (!cell) return;
            const line = document.createElement('div');
            const labelText = tableData!.headers[i] ? tableData!.headers[i] + ': ' : '';
            line.textContent = labelText + cell;
            block.appendChild(line);
          });
          list.appendChild(block);
          list.appendChild(document.createElement('hr'));
        });
      } else {
        const cards = this.extractCards(source);
        (cards || []).forEach(fields => {
          const block = document.createElement('div');
          block.className = 'report-receipt-row';
          fields.forEach(f => {
            const line = document.createElement('div');
            line.textContent = f;
            block.appendChild(line);
          });
          list.appendChild(block);
          list.appendChild(document.createElement('hr'));
        });
      }

      sheet.appendChild(list);

      const count = (tableData?.rows.length ?? this.extractCards(source)?.length ?? 0);
      const footer = document.createElement('div');
      footer.className = 'report-footer';
      footer.textContent = `تعداد: ${count}`;
      sheet.appendChild(footer);

      return sheet;
    }

    // ---- A4 / A5: فرم گزارش جدولی رسمی ----
    if (tableData && (tableData.headers.length > 0 || tableData.rows.length > 0)) {
      const table = document.createElement('table');
      table.className = 'report-table';

      if (tableData.headers.length > 0) {
        const thead = document.createElement('thead');
        const tr = document.createElement('tr');
        tableData.headers.forEach(h => {
          const th = document.createElement('th');
          th.textContent = h;
          tr.appendChild(th);
        });
        thead.appendChild(tr);
        table.appendChild(thead);
      }

      const tbody = document.createElement('tbody');
      tableData.rows.forEach((row, idx) => {
        const tr = document.createElement('tr');
        if (idx % 2 === 1) tr.className = 'report-row-alt';
        row.forEach(cell => {
          const td = document.createElement('td');
          td.textContent = cell;
          tr.appendChild(td);
        });
        tbody.appendChild(tr);
      });
      table.appendChild(tbody);
      sheet.appendChild(table);

      const footer = document.createElement('div');
      footer.className = 'report-footer';
      footer.textContent = `تعداد ردیف: ${tableData.rows.length}`;
      sheet.appendChild(footer);
    } else {
      const cards = this.extractCards(source);
      const grid = document.createElement('div');
      grid.className = 'report-card-grid';

      (cards || []).forEach(fields => {
        const block = document.createElement('div');
        block.className = 'report-card-block';
        fields.forEach(f => {
          const line = document.createElement('div');
          line.className = 'report-card-field';
          line.textContent = f;
          block.appendChild(line);
        });
        grid.appendChild(block);
      });

      sheet.appendChild(grid);

      const footer = document.createElement('div');
      footer.className = 'report-footer';
      footer.textContent = `تعداد: ${(cards || []).length}`;
      sheet.appendChild(footer);
    }

    const signLine = document.createElement('div');
    signLine.className = 'report-sign-line';
    signLine.innerHTML = '<span>امضای تنظیم‌کننده</span><span>امضای تایید‌کننده</span>';
    sheet.appendChild(signLine);

    return sheet;
  }

  print(title: string, element: HTMLElement): void {
    if (!element) return;

    const existing = document.getElementById('global-print-overlay');
    if (existing) existing.remove();

    let template = this.loadSavedTemplate();

    const overlay = document.createElement('div');
    overlay.id = 'global-print-overlay';

    const toolbar = document.createElement('div');
    toolbar.className = 'global-print-toolbar no-print';

    const label = document.createElement('label');
    label.textContent = 'قالب چاپ: ';

    const select = document.createElement('select');
    this.templates.forEach(t => {
      const opt = document.createElement('option');
      opt.value = t.key;
      opt.textContent = t.label;
      if (t.key === template) opt.selected = true;
      select.appendChild(opt);
    });
    select.addEventListener('change', () => {
      template = select.value as PrintTemplate;
      localStorage.setItem(TEMPLATE_STORAGE_KEY, template);
      this.applyPageSize(template);
      rerender();
    });
    label.appendChild(select);

    const printBtn = document.createElement('button');
    printBtn.textContent = 'چاپ';
    printBtn.className = 'btn-primary';
    printBtn.onclick = () => window.print();

    const closeBtn = document.createElement('button');
    closeBtn.textContent = 'بستن';
    closeBtn.className = 'btn-secondary';
    closeBtn.onclick = () => cleanup();

    toolbar.appendChild(label);
    toolbar.appendChild(printBtn);
    toolbar.appendChild(closeBtn);

    const printContainer = document.createElement('div');
    printContainer.className = 'global-print-container';

    const rerender = () => {
      printContainer.innerHTML = '';
      printContainer.appendChild(this.buildReportSheet(title, element, template));
    };
    rerender();

    overlay.appendChild(toolbar);
    overlay.appendChild(printContainer);

    document.body.appendChild(overlay);
    document.body.classList.add('printing-active');

    this.applyPageSize(template);

    const cleanup = () => {
      document.body.classList.remove('printing-active');
      overlay.remove();
      window.removeEventListener('afterprint', afterPrintHandler);
    };
    const afterPrintHandler = () => cleanup();
    window.addEventListener('afterprint', afterPrintHandler);
  }
}
