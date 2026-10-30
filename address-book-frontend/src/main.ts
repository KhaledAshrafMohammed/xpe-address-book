import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideZonelessChangeDetection } from '@angular/core';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { API_BASE_URL } from './app/core/services/address-book.service';

async function start(): Promise<void> {
  const response = await fetch('/config.json', { cache: 'no-store' });
  if (!response.ok) throw new Error('Cannot load application configuration.');
  const config: { apiBaseUrl?: unknown } = await response.json();
  if (typeof config.apiBaseUrl !== 'string' || !config.apiBaseUrl.startsWith('/') || config.apiBaseUrl.startsWith('//'))
    throw new Error('Configure a same-origin apiBaseUrl in config.json.');
  await bootstrapApplication(AppComponent, { providers: [
    provideZonelessChangeDetection(), provideHttpClient(), provideRouter(routes),
    { provide: API_BASE_URL, useValue: config.apiBaseUrl.replace(/\/$/, '') }
  ] });
}
start().catch(() => { document.body.textContent = 'The application could not start. Check config.json and reload the page.'; });
