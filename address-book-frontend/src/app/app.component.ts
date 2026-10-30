import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet],
  template: `<header class="site-header"><a routerLink="/companies" class="brand"><span class="brand-icon" aria-hidden="true">AB</span>Company Address Book</a><span class="header-caption">Companies & contacts</span></header>
    <main><router-outlet /></main><footer>Company Address Book · TSS technical assessment</footer>`
})
export class AppComponent {}
