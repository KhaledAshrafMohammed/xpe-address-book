using System.ComponentModel.DataAnnotations;

namespace AddressBook.Api.Contracts.Contacts;

public sealed record CreateContactRequest(
    [Required, StringLength(200)] string Name,
    [Required, StringLength(16)] string PhoneNumber,
    [Required] DateOnly? BirthDate);
