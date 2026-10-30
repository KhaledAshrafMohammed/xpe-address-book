import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AddressBookService, apiError } from '../../core/services/address-book.service';
import { companyName, fieldError, integer } from '../../core/services/form-validation';

@Component({
  selector: 'app-company-create', imports: [ReactiveFormsModule, RouterLink],
  template: `<a routerLink="/companies" class="back-link">← Companies</a><div class="page-heading"><div><p class="eyebrow">DIRECTORY</p><h1>Add company</h1><p class="muted">Set a contact capacity and an optional age limit.</p></div></div>
    <form class="panel form-panel" [formGroup]="form" (ngSubmit)="submit()" novalidate>
      @if (error()) { <div class="alert error" role="alert">{{ error() }}</div> }
      <label for="company-name">Company name</label><input id="company-name" formControlName="name" maxlength="200" dir="auto" aria-describedby="name-help name-error" [attr.aria-invalid]="!!message(form.controls.name, 'Company name')" />
      <p class="hint" id="name-help">Include a digit (0–9) immediately followed by §, ®, ™, ©, ʬ or @. Example: ExpertPeople2®.</p><p class="field-error" id="name-error">{{ message(form.controls.name, 'Company name') }}</p>
      <label for="capacity">Allowed contacts</label><input id="capacity" type="number" min="0" max="2147483647" step="1" formControlName="numContacts" aria-describedby="capacity-help capacity-error" />
      <p class="hint" id="capacity-help">Zero is allowed; no contacts can be added to a zero-capacity company.</p><p class="field-error" id="capacity-error">{{ message(form.controls.numContacts, 'Allowed contacts') }}</p>
      <label for="max-age">Maximum contact age <span class="muted">(optional)</span></label><input id="max-age" type="number" min="0" max="2147483647" step="1" formControlName="maxContactAge" aria-describedby="age-help age-error" />
      <p class="hint" id="age-help">Leave empty for no age restriction. The maximum age is inclusive.</p><p class="field-error" id="age-error">{{ message(form.controls.maxContactAge, 'Maximum age') }}</p>
      <div class="form-actions"><button type="submit" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Create company' }}</button><a class="button secondary" routerLink="/companies">Cancel</a></div>
    </form>`
})
export class CompanyCreateComponent {
  private readonly api = inject(AddressBookService); private readonly router = inject(Router);
  readonly saving = signal(false); readonly error = signal(''); readonly message = fieldError;
  readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200), companyName] }),
    numContacts: new FormControl<number | null>(null, [Validators.required, Validators.min(0), Validators.max(2147483647), integer]),
    maxContactAge: new FormControl<number | null>(null, [Validators.min(0), Validators.max(2147483647), integer])
  });
  submit(): void {
    this.form.markAllAsTouched(); if (this.form.invalid || this.saving()) return;
    this.saving.set(true); this.error.set(''); const value = this.form.getRawValue();
    this.api.createCompany({ name: value.name.trim(), numContacts: value.numContacts!, maxContactAge: value.maxContactAge })
      .pipe(finalize(() => this.saving.set(false))).subscribe({ next: () => { void this.router.navigate(['/companies']); }, error: e => this.error.set(apiError(e)) });
  }
}
