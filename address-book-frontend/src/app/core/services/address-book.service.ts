import { inject, Injectable, InjectionToken } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Company, Contact, CreateCompany, CreateContact, ImportSummary } from '../models/address-book';

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL');

@Injectable({ providedIn: 'root' })
export class AddressBookService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL)}/companies`;
  companies() { return this.http.get<Company[]>(this.url); }
  company(id: number) { return this.http.get<Company>(`${this.url}/${id}`); }
  createCompany(company: CreateCompany) { return this.http.post<Company>(this.url, company); }
  importCompanies(file: File) {
    const body = new FormData(); body.append('file', file);
    return this.http.post<ImportSummary>(`${this.url}/import`, body);
  }
  contacts(id: number) { return this.http.get<Contact[]>(`${this.url}/${id}/contacts`); }
  createContact(id: number, contact: CreateContact) { return this.http.post<Contact>(`${this.url}/${id}/contacts`, contact); }
}

export function apiError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) return 'Something went wrong. Please try again.';
  if (error.status === 0) return 'Cannot reach the server. Check that the API is running and try again.';
  if (error.status === 413) return 'The upload is too large. Select a TXT file up to 1 MiB.';
  const problem = error.error as { errors?: Record<string, string[]>; detail?: string; title?: string } | null;
  return (problem?.errors ? Object.values(problem.errors).flat().join(' ') : problem?.detail || problem?.title)
    || `The request failed (${error.status}). Please try again.`;
}
