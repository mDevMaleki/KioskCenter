import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { StyleService } from './app/services/style.service';

bootstrapApplication(AppComponent, appConfig)
  .then((appRef) => {
    // لود کردن استایل قبل از اجرای برنامه
    const styleService = appRef.injector.get(StyleService);
    styleService.loadStyle();
    console.log('Application started with dynamic styling');
  })
  .catch((err) => console.error(err));