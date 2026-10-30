using System.Globalization;
using AddressBook.Core.Entities;
using AddressBook.Core.Interfaces;
using AddressBook.Core.Validation;

namespace AddressBook.Core.Services;

public sealed record RejectedRow(int Line, string Reason);
public sealed record ImportSummary(int TotalRows, int ImportedRows, int SkippedDuplicates, IReadOnlyList<RejectedRow> RejectedRows);

public sealed class CompanyService(IAddressBookStore store)
{
    public async Task<Company> CreateAsync(string name, int numContacts, int? maxContactAge, CancellationToken cancellationToken)
    {
        CompanyRules.Validate(name, numContacts, maxContactAge);
        var company = new Company { Name = name.Trim(), NumContacts = numContacts, MaxContactAge = maxContactAge };
        if (!await store.TryAddCompanyAsync(company, cancellationToken))
            throw new ConflictException("A company with this exact name already exists.");
        return company;
    }

    public async Task<ImportSummary> ImportAsync(TextReader reader, CancellationToken cancellationToken)
    {
        var header = await reader.ReadLineAsync(cancellationToken);
        var columns = header?.TrimStart('\uFEFF').Split("\t\t", StringSplitOptions.None);
        if (columns is not { Length: 3 } ||
            !columns.Select(c => c.Trim().TrimStart('#')).SequenceEqual(["Company", "NumContacts", "MaxContactAge"]))
            throw new InputException("file", "Expected header: #Company, #NumContacts, #MaxContactAge separated by double tabs.");

        var candidates = new List<Company>();
        var rejected = new List<RejectedRow>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        int total = 0, duplicates = 0, lineNumber = 1;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (++total > 2000)
                throw new InputException("file", "A file may contain at most 2,000 data rows.");
            var values = line.Split("\t\t", StringSplitOptions.None);
            try
            {
                if (values.Length != 3 || values.Any(v => v.Contains('\t')))
                    throw new InputException("file", "Expected exactly three double-tab-separated columns; keep the last separator for an empty age.");
                var name = values[0].Trim();
                if (!int.TryParse(values[1].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var capacity))
                    throw new InputException("numContacts", "Contact capacity must be a non-negative integer.");
                int? maxAge = null;
                if (!string.IsNullOrWhiteSpace(values[2]))
                {
                    if (!int.TryParse(values[2].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var age))
                        throw new InputException("maxContactAge", "Maximum age must be a non-negative integer or empty.");
                    maxAge = age;
                }
                CompanyRules.Validate(name, capacity, maxAge);
                if (!seen.Add(name)) { duplicates++; continue; }
                candidates.Add(new Company { Name = name, NumContacts = capacity, MaxContactAge = maxAge });
            }
            catch (InputException ex) { rejected.Add(new RejectedRow(lineNumber, ex.Message)); }
        }

        var imported = 0;
        foreach (var company in candidates)
        {
            if (await store.TryAddCompanyAsync(company, cancellationToken)) imported++;
            else duplicates++;
        }
        return new ImportSummary(total, imported, duplicates, rejected);
    }
}
