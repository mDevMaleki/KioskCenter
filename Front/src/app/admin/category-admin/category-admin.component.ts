import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { KioskService } from '../../services/kiosk.service';

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

  constructor(private kioskService:KioskService){}

  ngOnInit(){
    this.loadCategories();
  }

  loadCategories(){
    this.kioskService.getCategories().subscribe(res=>{
      this.categories=res;
    });
  }

  saveCategory(){

    if(this.newCategory.id){

      this.kioskService.updateCategory(this.newCategory).subscribe(()=>{
        this.loadCategories();
        this.newCategory={id:null,name:''};
      });

    }else{

      this.kioskService.addCategory(this.newCategory).subscribe(()=>{
        this.loadCategories();
        this.newCategory={id:null,name:''};
      });

    }
  }

  editCategory(cat:any){
    this.newCategory={...cat};
  }

  deleteCategory(id:number){

    if(!confirm("حذف شود؟")) return;

    this.kioskService.deleteCategory(id).subscribe(()=>{
      this.loadCategories();
    });

  }

}
