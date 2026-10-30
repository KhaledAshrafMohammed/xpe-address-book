import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'companies' },
  { path: 'companies', loadComponent: () => import('./features/companies/company-list.component').then(m => m.CompanyListComponent), title: 'Companies · Address Book' },
  { path: 'companies/new', loadComponent: () => import('./features/companies/company-create.component').then(m => m.CompanyCreateComponent), title: 'Add company · Address Book' },
  { path: 'companies/import', loadComponent: () => import('./features/companies/company-import.component').then(m => m.CompanyImportComponent), title: 'Import companies · Address Book' },
  { path: 'companies/:id/contacts', loadComponent: () => import('./features/contacts/company-contacts.component').then(m => m.CompanyContactsComponent), title: 'Contacts · Address Book' },
  { path: '**', redirectTo: 'companies' }
];
