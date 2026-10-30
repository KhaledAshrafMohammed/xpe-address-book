import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { Company, Contact } from '../../core/models/address-book';
import { AddressBookService, apiError } from '../../core/services/address-book.service';
import { birthday, fieldError, nonBlank, utcToday } from '../../core/services/form-validation';

@Component({
  selector: 'app-company-contacts', imports: [ReactiveFormsModule, RouterLink, DatePipe],
  template: `<a routerLink="/companies" class="back-link">← Companies</a>
    @if (loading()) { <p class="panel" role="status">Loading contacts…</p> }
    @if (loadError()) { <div class="alert error" role="alert">{{ loadError() }} <button class="text-button" (click)="load()">Retry</button></div> }
    @if (company(); as company) { <div class="page-heading"><div><p class="eyebrow">COMPANY CONTACTS</p><h1 dir="auto">{{ company.name }}</h1><p class="muted">{{ company.contactCount }} of {{ company.numContacts }} contacts · {{ remaining() }} remaining · {{ company.maxContactAge === null ? 'No age limit' : 'Maximum age: ' + company.maxContactAge + ' years' }}</p></div></div>
      <div class="contacts-layout"><section class="panel"><div class="section-heading"><h2>Contacts</h2><span class="badge">{{ contacts().length }}</span></div>
        @if (contacts().length) { <div class="table-scroll"><table><thead><tr><th>Name</th><th>Phone number</th><th>Birthday</th></tr></thead><tbody>@for (contact of contacts(); track contact.id) { <tr><td dir="auto" class="company-name">{{ contact.name }}</td><td><a [href]="'tel:' + contact.phoneNumber">{{ contact.phoneNumber }}</a></td><td>{{ contact.birthDate | date:'dd MMM yyyy':'UTC' }}</td></tr> }</tbody></table></div> }
        @else { <div class="empty"><h3>No contacts yet</h3><p>Add the first contact using the form.</p></div> }
      </section>
      <form class="panel" [formGroup]="form" (ngSubmit)="submit()" novalidate><h2>Add contact</h2>
        @if (remaining() === 0) { <div class="alert" role="status">This company has reached its contact capacity.</div> }
        @if (success()) { <div class="alert success" role="status">{{ success() }}</div> }
        @if (saveError()) { <div class="alert error" role="alert">{{ saveError() }}</div> }
        <label for="contact-name">Contact name</label><input id="contact-name" formControlName="name" maxlength="200" autocomplete="name" dir="auto" aria-describedby="contact-name-error" /><p class="field-error" id="contact-name-error">{{ message(form.controls.name, 'Contact name') }}</p>
        <label for="phone">Phone number</label><input id="phone" type="tel" formControlName="phoneNumber" maxlength="16" placeholder="+201012345678" autocomplete="tel" aria-describedby="phone-help phone-error" /><p class="hint" id="phone-help">International format: + and 8–15 digits; no spaces.</p><p class="field-error" id="phone-error">{{ message(form.controls.phoneNumber, 'Phone number') }}</p>
        <label for="birthday">Birthday</label><input id="birthday" type="date" formControlName="birthDate" [max]="today" autocomplete="bday" aria-describedby="birthday-error" /><p class="field-error" id="birthday-error">{{ message(form.controls.birthDate, 'Birthday') }}</p>
        <button type="submit" [disabled]="saving() || remaining() === 0">{{ saving() ? 'Saving…' : 'Add contact' }}</button>
      </form></div>
    }`
})
export class CompanyContactsComponent implements OnInit {
  private readonly api = inject(AddressBookService); private readonly route = inject(ActivatedRoute);
  private readonly id = Number(this.route.snapshot.paramMap.get('id'));
  readonly company = signal<Company | null>(null); readonly contacts = signal<Contact[]>([]);
  readonly loading = signal(true); readonly saving = signal(false); readonly loadError = signal(''); readonly saveError = signal(''); readonly success = signal('');
  readonly today = utcToday(); readonly message = fieldError;
  readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [nonBlank, Validators.maxLength(200)] }),
    phoneNumber: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.pattern(/^\+[1-9][0-9]{7,14}$/)] }),
    birthDate: new FormControl('', { nonNullable: true, validators: [Validators.required, birthday(() => this.company()?.maxContactAge ?? null)] })
  });
  ngOnInit(): void { this.load(); }
  remaining(): number { const company = this.company(); return company ? Math.max(0, company.numContacts - company.contactCount) : 0; }
  load(): void {
    this.loading.set(true); this.loadError.set('');
    forkJoin({ company: this.api.company(this.id), contacts: this.api.contacts(this.id) }).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: data => { this.company.set(data.company); this.contacts.set(data.contacts); this.form.controls.birthDate.updateValueAndValidity(); },
      error: e => this.loadError.set(apiError(e))
    });
  }
  submit(): void {
    this.form.markAllAsTouched(); this.success.set(''); this.saveError.set('');
    if (this.remaining() === 0) { this.saveError.set('This company has reached its contact capacity.'); return; }
    if (this.form.invalid || this.saving()) return;
    this.saving.set(true); const value = this.form.getRawValue();
    this.api.createContact(this.id, { ...value, name: value.name.trim() }).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => { this.form.reset(); this.success.set('Contact added.'); this.load(); },
      error: e => { this.saveError.set(apiError(e)); this.load(); }
    });
  }
}
