import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { KioskService } from '../../services/kiosk.service';
import { PrintService } from '../../services/print.service';

@Component({
  selector: 'app-category-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './category-admin.component.html',
   styleUrls: ['./category-admin.component.css']
})
export class CategoryAdminComponent implements OnInit {

  categories:any[]=[];
  newCategory:any={id:null,name:''};
  searchCategory = '';

  get filteredCategories(): any[] {
    const q = this.searchCategory.trim().toLowerCase();
    return q ? this.categories.filter(c => c.name.toLowerCase().includes(q)) : this.categories;
  }

  constructor(private kioskService:KioskService, private printService: PrintService){}

  @ViewChild('printArea') printAreaRef?: ElementRef<HTMLElement>;

  printSection(title: string): void {
    if (this.printAreaRef) this.printService.print(title, this.printAreaRef.nativeElement);
  }

  ngOnInit(){
    this.loadCategories();
  }

  loadCategories(){
    this.kioskService.getCategories().subscribe({
      next: res => {
        this.categories=res;
      },
      error: err => console.error('خطا در دریافت دسته‌بندی‌ها', err)
    });
  }

  saveCategory(){

    if(this.newCategory.id){

      this.kioskService.updateCategory(this.newCategory).subscribe({
        next: () => {
          this.loadCategories();
          this.newCategory={id:null,name:''};
        },
        error: err => alert(err?.error?.message || 'خطا در ویرایش دسته‌بندی')
      });

    }else{

      this.kioskService.addCategory(this.newCategory).subscribe({
        next: () => {
          this.loadCategories();
          this.newCategory={id:null,name:''};
        },
        error: err => alert(err?.error?.message || 'خطا در افزودن دسته‌بندی')
      });

    }
  }

  editCategory(cat:any){
    this.newCategory={...cat};
  }

  deleteCategory(id:number){

    if(!confirm("حذف شود؟")) return;

    this.kioskService.deleteCategory(id).subscribe({
      next: () => this.loadCategories(),
      error: err => alert(err?.error?.message || 'خطا در حذف دسته‌بندی')
    });

  }

}
