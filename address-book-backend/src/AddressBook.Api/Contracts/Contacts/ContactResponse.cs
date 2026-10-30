using AddressBook.Core.Entities;

namespace AddressBook.Api.Contracts.Contacts;

public sealed record ContactResponse(int Id, int CompanyId, string Name, string PhoneNumber, DateOnly BirthDate)
{
    public static ContactResponse From(Contact contact) => new(contact.Id, contact.CompanyId, contact.Name, contact.PhoneNumber, contact.BirthDate);
}
