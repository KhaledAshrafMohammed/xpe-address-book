import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const companyName: ValidatorFn = control => {
  const value = String(control.value ?? '');
  return value.trim().length > 0 && /[0-9][§®™©ʬ@]/u.test(value) && !/[\u0000-\u001f\u007f-\u009f]/u.test(value) ? null : { companyName: true };
};
export const nonBlank: ValidatorFn = control => String(control.value ?? '').trim().length ? null : { required: true };
export const integer: ValidatorFn = control => control.value == null || Number.isInteger(control.value) ? null : { integer: true };
export function utcToday(): string { return new Date().toISOString().slice(0, 10); }
export function birthday(maxAge: () => number | null): ValidatorFn {
  return control => {
    const value = String(control.value ?? '');
    if (!value) return null;
    if (!/^\d{4}-\d{2}-\d{2}$/.test(value) || Number.isNaN(Date.parse(value))) return { birthday: true };
    const today = utcToday();
    if (value > today) return { future: true };
    const birth = new Date(`${value}T00:00:00Z`), now = new Date(`${today}T00:00:00Z`);
    const anniversary = Date.UTC(now.getUTCFullYear(), birth.getUTCMonth(), Math.min(birth.getUTCDate(), new Date(Date.UTC(now.getUTCFullYear(), birth.getUTCMonth() + 1, 0)).getUTCDate()));
    const age = now.getUTCFullYear() - birth.getUTCFullYear() - (anniversary > now.getTime() ? 1 : 0);
    const limit = maxAge();
    return limit !== null && age > limit ? { maxAge: true } : null;
  };
}
export function fieldError(control: AbstractControl, label: string): string {
  if (!control.touched || !control.errors) return '';
  const errors: ValidationErrors = control.errors;
  if (errors['required']) return `${label} is required.`;
  if (errors['maxlength']) return `${label} is too long.`;
  if (errors['companyName']) return 'Include an ASCII digit directly followed by §, ®, ™, ©, ʬ or @.';
  if (errors['min'] || errors['max'] || errors['integer']) return 'Enter a whole number from 0 to 2147483647.';
  if (errors['pattern']) return 'Use + followed by 8–15 digits, with no spaces.';
  if (errors['future']) return 'Birthday cannot be in the future.';
  if (errors['maxAge']) return 'Contact age exceeds this company’s maximum age.';
  return 'Enter a valid birthday.';
}
