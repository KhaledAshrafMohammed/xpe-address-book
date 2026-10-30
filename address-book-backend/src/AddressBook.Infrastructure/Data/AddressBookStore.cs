using AddressBook.Core.Entities;
using AddressBook.Core.Interfaces;
using AddressBook.Core.Validation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AddressBook.Infrastructure.Data;

public sealed class AddressBookStore(AddressBookDbContext db) : IAddressBookStore
{
    public async Task<IReadOnlyList<CompanyInfo>> GetCompaniesAsync(CancellationToken cancellationToken) =>
        await db.Companies.AsNoTracking().OrderBy(c => c.Name)
            .Select(c => new CompanyInfo(c.Id, c.Name, c.NumContacts, c.MaxContactAge, c.Contacts.Count)).ToListAsync(cancellationToken);

    public Task<CompanyInfo?> GetCompanyAsync(int id, CancellationToken cancellationToken) =>
        db.Companies.AsNoTracking().Where(c => c.Id == id)
            .Select(c => new CompanyInfo(c.Id, c.Name, c.NumContacts, c.MaxContactAge, c.Contacts.Count)).SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> TryAddCompanyAsync(Company company, CancellationToken cancellationToken)
    {
        if (await db.Companies.AnyAsync(c => c.Name == company.Name, cancellationToken)) return false;
        db.Companies.Add(company);
        try { await db.SaveChangesAsync(cancellationToken); return true; }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            db.Entry(company).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<IReadOnlyList<Contact>> GetContactsAsync(int companyId, CancellationToken cancellationToken)
    {
        if (!await db.Companies.AnyAsync(c => c.Id == companyId, cancellationToken))
            throw new NotFoundException("Company not found.");
        return await db.Contacts.AsNoTracking().Where(c => c.CompanyId == companyId).OrderBy(c => c.Name).ThenBy(c => c.Id).ToListAsync(cancellationToken);
    }

    public async Task AddContactAsync(Contact contact, DateOnly today, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Serialize writers on the parent before counting, including when the company has no contacts yet.
        var company = await db.Companies.FromSqlInterpolated($"SELECT * FROM [Companies] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {contact.CompanyId}")
            .SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Company not found.");
        var count = await db.Contacts.CountAsync(c => c.CompanyId == company.Id, cancellationToken);
        ContactRules.ValidateForCompany(company, count, contact, today);
        db.Contacts.Add(contact);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
