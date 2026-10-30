using AddressBook.Core.Entities;
using AddressBook.Core.Interfaces;

namespace AddressBook.Api.Contracts.Companies;

public sealed record CompanyResponse(int Id, string Name, int NumContacts, int? MaxContactAge, int ContactCount)
{
    public static CompanyResponse From(CompanyInfo company) => new(company.Id, company.Name, company.NumContacts, company.MaxContactAge, company.ContactCount);
    public static CompanyResponse From(Company company) => new(company.Id, company.Name, company.NumContacts, company.MaxContactAge, 0);
}
