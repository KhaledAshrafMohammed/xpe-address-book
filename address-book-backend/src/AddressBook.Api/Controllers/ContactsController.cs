using AddressBook.Api.Contracts.Contacts;
using AddressBook.Core.Interfaces;
using AddressBook.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace AddressBook.Api.Controllers;

[ApiController]
[Route("api/companies/{id:int}/contacts")]
public sealed class ContactsController(IAddressBookStore store, ContactService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContactResponse>>> GetAll(int id, CancellationToken cancellationToken) =>
        Ok((await store.GetContactsAsync(id, cancellationToken)).Select(ContactResponse.From));

    [HttpPost]
    public async Task<ActionResult<ContactResponse>> Create(int id, CreateContactRequest request, CancellationToken cancellationToken)
    {
        var contact = await service.CreateAsync(id, request.Name, request.PhoneNumber, request.BirthDate!.Value, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id }, ContactResponse.From(contact));
    }
}
