import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ImportSummary } from '../../core/models/address-book';
import { AddressBookService, apiError } from '../../core/services/address-book.service';

@Component({
  selector: 'app-company-import', imports: [RouterLink],
  template: `<a routerLink="/companies" class="back-link">← Companies</a><div class="page-heading"><div><p class="eyebrow">DIRECTORY</p><h1>Import companies</h1><p class="muted">Upload a double-tab-separated UTF-8 TXT file.</p></div></div>
    <section class="panel form-panel"><h2>Choose a file</h2><p>Columns: Company, NumContacts, MaxContactAge. Include the header and keep the final double tab when age is empty.</p><p class="hint">Up to 1 MiB and 2,000 data rows. Existing company names are skipped; existing records stay unchanged.</p>
      <label for="file">TXT file</label><input id="file" type="file" accept=".txt,text/plain" (change)="select($event)" [disabled]="uploading()" />
      @if (error()) { <div class="alert error" role="alert">{{ error() }}</div> }
      <div class="form-actions"><button (click)="upload()" [disabled]="uploading() || !file()">{{ uploading() ? 'Importing…' : 'Import companies' }}</button><a class="button secondary" routerLink="/companies">Back to companies</a></div>
    </section>
    @if (summary(); as result) { <section class="panel" aria-live="polite"><div class="section-heading"><h2>Import complete</h2><a routerLink="/companies">View refreshed directory →</a></div>
      <p>{{ result.totalRows }} data rows: <strong>{{ result.importedRows }} imported</strong>, {{ result.skippedDuplicates }} duplicates skipped, {{ result.rejectedCount }} rejected.</p>
      @if (result.rejectedRows.length) { <h3>Rejected rows</h3><ul class="rejections">@for (row of result.rejectedRows; track row.line) { <li><strong>Line {{ row.line }}:</strong> {{ row.reason }}</li> }</ul> }
    </section> }`
})
export class CompanyImportComponent {
  private readonly api = inject(AddressBookService);
  readonly file = signal<File | null>(null); readonly uploading = signal(false); readonly error = signal(''); readonly summary = signal<ImportSummary | null>(null);
  select(event: Event): void {
    this.error.set(''); this.summary.set(null); this.file.set(null);
    const file = (event.target as HTMLInputElement).files?.[0]; if (!file) return;
    if (!/\.txt$/i.test(file.name)) { this.error.set('Choose a .txt file.'); return; }
    if (!file.size || file.size > 1024 * 1024) { this.error.set('Choose a non-empty TXT file up to 1 MiB.'); return; }
    this.file.set(file);
  }
  upload(): void {
    const file = this.file(); if (!file || this.uploading()) return;
    this.error.set(''); this.summary.set(null); this.uploading.set(true);
    this.api.importCompanies(file).pipe(finalize(() => this.uploading.set(false))).subscribe({ next: data => this.summary.set(data), error: e => this.error.set(apiError(e)) });
  }
}
