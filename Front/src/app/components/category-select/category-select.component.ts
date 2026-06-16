import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { StyleService, RestaurantStyle } from '../../services/style.service';

@Component({
  selector: 'app-category-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './category-select.component.html',
  styleUrls: ['./category-select.component.css']
})
export class CategorySelectComponent implements OnInit, OnDestroy {
  restaurantName = '';
  logoUrl = '';
  backgroundImageUrl = '';
  private styleSubscription: Subscription | null = null;

  constructor(private router: Router, private styleService: StyleService) {}

  ngOnInit(): void {
    this.styleSubscription = this.styleService.style$.subscribe(style => {
      if (style) {
        this.applyStyle(style);
      }
    });
  }

  ngOnDestroy(): void {
    this.styleSubscription?.unsubscribe();
  }

  private applyStyle(style: RestaurantStyle): void {
    this.restaurantName = style.restaurantName;
    this.logoUrl = style.logoUrl ? this.styleService.resolveAssetUrl(style.logoUrl) : '';
    this.backgroundImageUrl = style.backgroundImage
      ? this.styleService.resolveAssetUrl(style.backgroundImage)
      : 'assets/images/start.png';
  }

  goNext(): void {
    this.router.navigate(['/order-type']);
  }
}
