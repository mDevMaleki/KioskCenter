import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-category-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './category-select.component.html',
  styleUrls: ['./category-select.component.css']
})
export class CategorySelectComponent {

  constructor(private router: Router) {}

  goNext(): void {
    this.router.navigate(['/order-type']);
  }
}
