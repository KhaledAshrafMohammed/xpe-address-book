using AddressBook.Core.Entities;

namespace AddressBook.Core.Interfaces;

public sealed record CompanyInfo(int Id, string Name, int NumContacts, int? MaxContactAge, int ContactCount);

public interface IAddressBookStore
{
    Task<IReadOnlyList<CompanyInfo>> GetCompaniesAsync(CancellationToken cancellationToken);
    Task<CompanyInfo?> GetCompanyAsync(int id, CancellationToken cancellationToken);
    Task<bool> TryAddCompanyAsync(Company company, CancellationToken cancellationToken);
    Task<IReadOnlyList<Contact>> GetContactsAsync(int companyId, CancellationToken cancellationToken);
    Task AddContactAsync(Contact contact, DateOnly today, CancellationToken cancellationToken);
}
