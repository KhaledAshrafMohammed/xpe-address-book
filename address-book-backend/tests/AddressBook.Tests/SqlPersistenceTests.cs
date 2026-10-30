using AddressBook.Core.Entities;
using AddressBook.Core.Services;
using AddressBook.Core.Validation;
using AddressBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AddressBook.Tests;

[Collection("SqlServer")]
public sealed class SqlPersistenceTests(SqlDatabaseFixture fixture)
{
    [SqlServerFact]
    public async Task Unique_names_are_case_sensitive_and_unicode_is_preserved()
    {
        await using var db = fixture.CreateContext(); var store = new AddressBookStore(db);
        var name = "™جدير1®" + Guid.NewGuid().ToString("N");
        Assert.True(await store.TryAddCompanyAsync(new Company { Name = name, NumContacts = 1 }, default));
        Assert.False(await store.TryAddCompanyAsync(new Company { Name = name, NumContacts = 5 }, default));
        Assert.Equal(name, (await db.Companies.SingleAsync(c => c.Name == name)).Name);
        var suffix = Guid.NewGuid().ToString("N");
        db.Companies.AddRange(new Company { Name = "Case1®" + suffix }, new Company { Name = "case1®" + suffix });
        await db.SaveChangesAsync();
        db.Companies.Add(new Company { Name = name });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task Concurrent_duplicate_inserts_create_only_one_company()
    {
        var name = "Concurrent1®" + Guid.NewGuid().ToString("N");
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var db = fixture.CreateContext();
            return await new AddressBookStore(db).TryAddCompanyAsync(new Company { Name = name, NumContacts = 1 }, default);
        }));
        Assert.Equal(1, results.Count(r => r));
    }

    [SqlServerFact]
    public async Task Concurrent_contact_creation_never_exceeds_capacity()
    {
        int id;
        await using (var db = fixture.CreateContext())
            id = (await new CompanyService(new AddressBookStore(db)).CreateAsync("Capacity1®" + Guid.NewGuid().ToString("N"), 3, null, default)).Id;
        var results = await Task.WhenAll(Enumerable.Range(0, 12).Select(async i =>
        {
            await using var db = fixture.CreateContext();
            try
            {
                await new ContactService(new AddressBookStore(db), TimeProvider.System).CreateAsync(id, $"Contact {i}", "+201012345678", new DateOnly(2000, 1, 1), default);
                return true;
            }
            catch (ConflictException) { return false; }
        }));
        Assert.Equal(3, results.Count(r => r));
        await using var verification = fixture.CreateContext();
        Assert.Equal(3, await verification.Contacts.CountAsync(c => c.CompanyId == id));
    }

    [SqlServerFact]
    public async Task Negative_capacity_and_age_are_database_constraint_violations()
    {
        await using var db = fixture.CreateContext();
        db.Companies.Add(new Company { Name = "Negative1®", NumContacts = -1 });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        db.Companies.Add(new Company { Name = "NegativeAge1®", MaxContactAge = -1 });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task Foreign_key_and_phone_constraint_are_enforced()
    {
        await using var db = fixture.CreateContext();
        db.Contacts.Add(new Contact { CompanyId = int.MaxValue, Name = "Orphan", PhoneNumber = "+201012345678", BirthDate = new DateOnly(2000, 1, 1) });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        var company = new Company { Name = "Phone1®" + Guid.NewGuid().ToString("N"), NumContacts = 1 };
        db.Companies.Add(company); await db.SaveChangesAsync();
        db.Contacts.Add(new Contact { CompanyId = company.Id, Name = "Bad phone", PhoneNumber = "abc", BirthDate = new DateOnly(2000, 1, 1) });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task Company_with_contacts_cannot_be_deleted()
    {
        int id;
        await using (var db = fixture.CreateContext())
        {
            var company = await new CompanyService(new AddressBookStore(db)).CreateAsync("Delete1®" + Guid.NewGuid().ToString("N"), 1, null, default);
            id = company.Id;
            await new ContactService(new AddressBookStore(db), TimeProvider.System).CreateAsync(id, "Contact", "+201012345678", new DateOnly(2000, 1, 1), default);
        }
        await using var context = fixture.CreateContext();
        context.Companies.Remove(await context.Companies.SingleAsync(c => c.Id == id));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
