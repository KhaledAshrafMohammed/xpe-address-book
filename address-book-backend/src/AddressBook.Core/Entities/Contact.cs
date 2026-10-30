namespace AddressBook.Core.Entities;

public sealed class Contact
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public required string Name { get; set; }
    public required string PhoneNumber { get; set; }
    public DateOnly BirthDate { get; set; }
    public Company Company { get; set; } = null!;
}
