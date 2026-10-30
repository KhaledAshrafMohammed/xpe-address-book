using AddressBook.Core.Entities;
using AddressBook.Core.Interfaces;
using AddressBook.Core.Validation;

namespace AddressBook.Tests;

internal sealed class MemoryStore : IAddressBookStore
{
    public List<Company> Companies { get; } = [];
    public List<Contact> Contacts { get; } = [];
    public Task<bool> TryAddCompanyAsync(Company company, CancellationToken cancellationToken)
    {
        if (Companies.Any(c => c.Name == company.Name)) return Task.FromResult(false);
        company.Id = Companies.Count + 1; Companies.Add(company); return Task.FromResult(true);
    }
    public Task<IReadOnlyList<CompanyInfo>> GetCompaniesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CompanyInfo>>(Companies.Select(Info).ToList());
    public Task<CompanyInfo?> GetCompanyAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Companies.Where(c => c.Id == id).Select(Info).SingleOrDefault());
    public Task<IReadOnlyList<Contact>> GetContactsAsync(int companyId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Contact>>(Contacts.Where(c => c.CompanyId == companyId).ToList());
    public Task AddContactAsync(Contact contact, DateOnly today, CancellationToken cancellationToken)
    {
        var company = Companies.SingleOrDefault(c => c.Id == contact.CompanyId) ?? throw new NotFoundException("Company not found.");
        ContactRules.ValidateForCompany(company, Contacts.Count(c => c.CompanyId == company.Id), contact, today);
        contact.Id = Contacts.Count + 1; Contacts.Add(contact); return Task.CompletedTask;
    }
    private CompanyInfo Info(Company c) => new(c.Id, c.Name, c.NumContacts, c.MaxContactAge, Contacts.Count(contact => contact.CompanyId == c.Id));
}
