import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PrinterService } from '../../services/printer.service';
import { PrinterSetting } from '../../models/printer.model';

@Component({
  selector: 'app-printer-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './printer-admin.component.html',
  styleUrls: ['./printer-admin.component.css']
})
export class PrinterAdminComponent implements OnInit {
  printers: PrinterSetting[] = [];
  installedPrinters: string[] = [];
  editingPrinter: PrinterSetting | null = null;
  showForm = false;
  isEditing = false;
  searchPrinter = '';

  get filteredPrinters(): PrinterSetting[] {
    const q = this.searchPrinter.trim().toLowerCase();
    return q ? this.printers.filter(p =>
      p.name.toLowerCase().includes(q) ||
      p.printerName.toLowerCase().includes(q)
    ) : this.printers;
  }
  
  printerTypes = [
    { value: 'Receipt', label: 'پرینتر فیش' },
    { value: 'Kitchen', label: 'پرینتر آشپزخانه' },
    { value: 'Label', label: 'پرینتر برچسب' }
  ];

  constructor(private printerService: PrinterService) {}

  ngOnInit() {
    this.loadPrinters();
    this.loadInstalledPrinters();
  }

  loadPrinters() {
    this.printerService.getPrinters().subscribe({
      next: (res) => this.printers = res,
      error: (err) => console.error('Error loading printers:', err)
    });
  }

  loadInstalledPrinters() {
    this.printerService.getInstalledPrinters().subscribe({
      next: (res) => this.installedPrinters = res,
      error: (err) => console.error('Error loading installed printers:', err)
    });
  }

  addPrinter() {
    this.editingPrinter = {
      id: 0,
      name: '',
      printerName: '',
      printerType: 'Receipt',
      isActive: true,
      priority: this.printers.length,
      categories: [],
      productIds: [],
      createdAt: new Date()
    };
    this.isEditing = false;
    this.showForm = true;
  }

  editPrinter(printer: PrinterSetting) {
    this.editingPrinter = { ...printer };
    this.isEditing = true;
    this.showForm = true;
  }

  deletePrinter(id: number) {
    if (confirm('آیا از حذف این پرینتر اطمینان دارید؟')) {
      this.printerService.deletePrinter(id).subscribe({
        next: () => this.loadPrinters(),
        error: (err) => console.error('Error deleting printer:', err)
      });
    }
  }

  testPrinter(id: number) {
    this.printerService.testPrinter(id).subscribe({
      next: (res) => alert(res.message),
      error: (err) => alert('خطا در تست پرینتر')
    });
  }

  savePrinter() {
    if (!this.editingPrinter) return;

    if (this.isEditing) {
      this.printerService.updatePrinter(this.editingPrinter.id, this.editingPrinter).subscribe({
        next: () => {
          this.loadPrinters();
          this.showForm = false;
          this.editingPrinter = null;
        },
        error: (err) => console.error('Error updating printer:', err)
      });
    } else {
      this.printerService.createPrinter(this.editingPrinter).subscribe({
        next: () => {
          this.loadPrinters();
          this.showForm = false;
          this.editingPrinter = null;
        },
        error: (err) => console.error('Error creating printer:', err)
      });
    }
  }

  cancelForm() {
    this.showForm = false;
    this.editingPrinter = null;
  }
}