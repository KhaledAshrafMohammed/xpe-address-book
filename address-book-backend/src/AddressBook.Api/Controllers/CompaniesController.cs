using System.Text;
using AddressBook.Api.Contracts.Companies;
using AddressBook.Core.Interfaces;
using AddressBook.Core.Services;
using AddressBook.Core.Validation;
using Microsoft.AspNetCore.Mvc;

namespace AddressBook.Api.Controllers;

[ApiController]
[Route("api/companies")]
public sealed class CompaniesController(IAddressBookStore store, CompanyService service) : ControllerBase
{
    public const int MaxFileBytes = 1024 * 1024;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CompanyResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok((await store.GetCompaniesAsync(cancellationToken)).Select(CompanyResponse.From));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CompanyResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var company = await store.GetCompanyAsync(id, cancellationToken) ?? throw new NotFoundException("Company not found.");
        return Ok(CompanyResponse.From(company));
    }

    [HttpPost]
    public async Task<ActionResult<CompanyResponse>> Create(CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var company = await service.CreateAsync(request.Name, request.NumContacts!.Value, request.MaxContactAge, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = company.Id }, CompanyResponse.From(company));
    }

    [HttpPost("import")]
    [RequestSizeLimit(MaxFileBytes + 16 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileBytes + 16 * 1024)]
    public async Task<ActionResult<ImportResponse>> Import(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) throw new InputException("file", "Choose a non-empty TXT file.");
        if (!string.Equals(Path.GetExtension(file.FileName), ".txt", StringComparison.OrdinalIgnoreCase))
            throw new InputException("file", "Only .txt files are supported.");
        if (file.Length > MaxFileBytes) throw new InputException("file", "The file must be 1 MiB or smaller.");

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: false);
        try
        {
            var result = await service.ImportAsync(reader, cancellationToken);
            return Ok(new ImportResponse(result.TotalRows, result.ImportedRows, result.SkippedDuplicates,
                result.RejectedRows.Count, result.RejectedRows.Select(r => new RejectedRowResponse(r.Line, r.Reason)).ToArray()));
        }
        catch (DecoderFallbackException) { throw new InputException("file", "Save the TXT file using UTF-8 encoding."); }
    }
}
