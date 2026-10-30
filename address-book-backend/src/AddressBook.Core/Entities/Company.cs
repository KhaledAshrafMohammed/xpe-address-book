namespace AddressBook.Core.Entities;

public sealed class Company
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int NumContacts { get; set; }
    public int? MaxContactAge { get; set; }
    public List<Contact> Contacts { get; set; } = [];
}
