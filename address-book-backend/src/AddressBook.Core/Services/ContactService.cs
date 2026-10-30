using AddressBook.Core.Entities;
using AddressBook.Core.Interfaces;
using AddressBook.Core.Validation;

namespace AddressBook.Core.Services;

public sealed class ContactService(IAddressBookStore store, TimeProvider clock)
{
    public async Task<Contact> CreateAsync(int companyId, string name, string phoneNumber, DateOnly birthDate, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var contact = new Contact { CompanyId = companyId, Name = name.Trim(), PhoneNumber = phoneNumber.Trim(), BirthDate = birthDate };
        ContactRules.Validate(contact, today);
        await store.AddContactAsync(contact, today, cancellationToken);
        return contact;
    }
}
