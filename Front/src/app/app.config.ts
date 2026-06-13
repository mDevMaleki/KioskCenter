import { ApplicationConfig, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { routes } from './app.routes';
import { StyleService } from './services/style.service';
import { HashLocationStrategy, LocationStrategy } from '@angular/common';  // <-- اضافه کنید
export function initializeApp(styleService: StyleService) {
  return (): Promise<void> => {
    return new Promise((resolve) => {
      styleService.loadStyle();
      styleService.style$.subscribe({
        next: (style) => {
          if (style) {
            // اعمال استایل و فونت
            const root = document.documentElement;
            root.style.setProperty('--primary-color', style.primaryColor);
            root.style.setProperty('--secondary-color', style.secondaryColor);
            root.style.setProperty('--text-color', style.textColor);
            root.style.setProperty('--button-color', style.buttonColor);
            root.style.setProperty('--button-hover-color', style.buttonHoverColor);
            
            // اعمال فونت
            if (style.customFontUrl) {
              const fontFace = `
                @font-face {
                  font-family: '${style.fontName}';
                  src: url('${style.customFontUrl}') format('truetype');
                  font-weight: ${style.fontWeight};
                  font-style: ${style.fontStyle};
                }
              `;
              let styleElement = document.getElementById('dynamic-font-face');
              if (!styleElement) {
                styleElement = document.createElement('style');
                styleElement.id = 'dynamic-font-face';
                document.head.appendChild(styleElement);
              }
              styleElement.textContent = fontFace;
              document.body.style.fontFamily = `'${style.fontName}', ${style.fontFamily}`;
            } else {
              document.body.style.fontFamily = style.fontFamily;
            }
            
            if (style.backgroundColor) {
              document.body.style.background = style.backgroundColor;
            }
            document.title = style.restaurantName;
          }
          resolve();
        },
        error: () => resolve()
      });
    });
  };
}
export const appConfig: ApplicationConfig = {
  providers: [
     provideRouter(routes),
    { provide: LocationStrategy, useClass: HashLocationStrategy },  // <-- این خط را اضافه کنید
  
    provideHttpClient(),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeApp,
      deps: [StyleService],
      multi: true
    }
  ]
};