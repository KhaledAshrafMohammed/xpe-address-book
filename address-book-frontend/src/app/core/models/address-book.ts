export interface Company { id: number; name: string; numContacts: number; maxContactAge: number | null; contactCount: number; }
export interface Contact { id: number; companyId: number; name: string; phoneNumber: string; birthDate: string; }
export interface CreateCompany { name: string; numContacts: number; maxContactAge: number | null; }
export interface CreateContact { name: string; phoneNumber: string; birthDate: string; }
export interface ImportSummary { totalRows: number; importedRows: number; skippedDuplicates: number; rejectedCount: number; rejectedRows: { line: number; reason: string }[]; }
