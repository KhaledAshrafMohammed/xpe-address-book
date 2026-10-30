import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { Company } from '../../core/models/address-book';
import { AddressBookService, apiError } from '../../core/services/address-book.service';

@Component({
  selector: 'app-company-list', imports: [RouterLink],
  template: `<div class="page-heading"><div><p class="eyebrow">DIRECTORY</p><h1>Companies</h1><p class="muted">Manage companies and the people you connect with.</p></div>
    <div class="actions"><a class="button secondary" routerLink="/companies/import">Import companies</a><a class="button" routerLink="/companies/new">+ Add company</a></div></div>
    @if (error()) { <div class="alert error" role="alert">{{ error() }} <button class="text-button" (click)="load()">Retry</button></div> }
    @if (loading()) { <p role="status" class="panel">Loading companies…</p> }
    @else if (!error()) {
      <section class="panel"><div class="section-heading"><h2>Company directory</h2><span class="badge">{{ companies().length }} companies</span></div>
      @if (companies().length) { <div class="table-scroll"><table><thead><tr><th>Company name</th><th>Allowed contacts</th><th>Existing contacts</th><th>Maximum age</th><th>Actions</th></tr></thead><tbody>
        @for (company of companies(); track company.id) { <tr><td class="company-name" dir="auto">{{ company.name }}</td><td>{{ company.numContacts }}</td><td>{{ company.contactCount }} <span class="muted">/ {{ company.numContacts }}</span></td><td>{{ company.maxContactAge === null ? 'No limit' : company.maxContactAge + ' years' }}</td><td><a [routerLink]="['/companies', company.id, 'contacts']">View contacts →</a></td></tr> }
      </tbody></table></div> }
      @else { <div class="empty"><h3>Your directory is empty</h3><p>Add a company or import the supplied TXT file to get started.</p><a routerLink="/companies/import">Import companies →</a></div> }</section>
    }`
})
export class CompanyListComponent implements OnInit {
  private readonly api = inject(AddressBookService);
  readonly companies = signal<Company[]>([]); readonly loading = signal(true); readonly error = signal('');
  ngOnInit(): void { this.load(); }
  load(): void {
    this.loading.set(true); this.error.set('');
    this.api.companies().pipe(finalize(() => this.loading.set(false))).subscribe({ next: data => this.companies.set(data), error: e => this.error.set(apiError(e)) });
  }
}
