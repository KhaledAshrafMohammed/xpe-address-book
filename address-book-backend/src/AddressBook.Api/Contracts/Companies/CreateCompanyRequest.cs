using System.ComponentModel.DataAnnotations;

namespace AddressBook.Api.Contracts.Companies;

public sealed record CreateCompanyRequest(
    [Required, StringLength(200)] string Name,
    [Required, Range(0, int.MaxValue)] int? NumContacts,
    [Range(0, int.MaxValue)] int? MaxContactAge);
