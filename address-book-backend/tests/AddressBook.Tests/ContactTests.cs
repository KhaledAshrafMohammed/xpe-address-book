using AddressBook.Core.Entities;
using AddressBook.Core.Services;
using AddressBook.Core.Validation;
using Xunit;

namespace AddressBook.Tests;

public sealed class ContactTests
{
    private static readonly DateOnly Today = new(2026, 10, 30);
    private static Contact Contact(DateOnly? birth = null, string phone = "+201012345678", string name = "Contact") =>
        new() { CompanyId = 1, Name = name, PhoneNumber = phone, BirthDate = birth ?? new DateOnly(2000, 10, 30) };

    [Theory]
    [InlineData("+201012345678")]
    [InlineData("+12345678")]
    [InlineData("+123456789012345")]
    public void Accepts_international_phone(string phone) => ContactRules.Validate(Contact(phone: phone), Today);

    [Theory]
    [InlineData("01012345678")]
    [InlineData("+012345678")]
    [InlineData("+20 1012345678")]
    [InlineData("+١٢٣٤٥٦٧٨٩")]
    [InlineData("+1234567")]
    [InlineData("+1234567890123456")]
    [InlineData("+12345678\n")]
    [InlineData("")]
    public void Rejects_invalid_phone(string phone) => Assert.Throws<InputException>(() => ContactRules.Validate(Contact(phone: phone), Today));

    [Fact]
    public void Required_fields_and_future_birthdays_are_rejected()
    {
        Assert.Throws<InputException>(() => ContactRules.Validate(Contact(name: " "), Today));
        Assert.Throws<InputException>(() => ContactRules.Validate(Contact(birth: Today.AddDays(1)), Today));
        Assert.Throws<InputException>(() => ContactRules.Validate(Contact(birth: default(DateOnly)), Today));
        ContactRules.Validate(Contact(birth: Today), Today);
    }

    [Theory]
    [InlineData(2000, 10, 30, 26)]
    [InlineData(2000, 10, 31, 25)]
    [InlineData(2000, 10, 29, 26)]
    public void Age_uses_complete_years(int year, int month, int day, int expected) => Assert.Equal(expected, ContactRules.AgeOn(new DateOnly(year, month, day), Today));

    [Fact]
    public void Leap_day_birthday_uses_february_28_in_non_leap_years()
    {
        Assert.Equal(20, ContactRules.AgeOn(new DateOnly(2004, 2, 29), new DateOnly(2025, 2, 27)));
        Assert.Equal(21, ContactRules.AgeOn(new DateOnly(2004, 2, 29), new DateOnly(2025, 2, 28)));
    }

    [Fact]
    public void Exact_maximum_age_is_allowed_but_older_is_rejected()
    {
        var company = new Company { Name = "A1®", NumContacts = 2, MaxContactAge = 26 };
        ContactRules.ValidateForCompany(company, 0, Contact(), Today);
        Assert.Throws<InputException>(() => ContactRules.ValidateForCompany(company, 0, Contact(new DateOnly(1999, 10, 30)), Today));
        company.MaxContactAge = null;
        ContactRules.ValidateForCompany(company, 0, Contact(new DateOnly(1900, 1, 1)), Today);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    public void Capacity_is_enforced(int capacity, int count) => Assert.Throws<ConflictException>(() => ContactRules.ValidateForCompany(new Company { Name = "A1®", NumContacts = capacity }, count, Contact(), Today));

    [Fact]
    public async Task Service_saves_valid_contact_and_reports_missing_company()
    {
        var store = new MemoryStore();
        await new CompanyService(store).CreateAsync("A1®", 1, null, default);
        var service = new ContactService(store, new FixedClock());
        var result = await service.CreateAsync(1, " Contact ", "+201012345678", new DateOnly(2000, 10, 30), default);
        Assert.Equal("Contact", result.Name); Assert.Single(store.Contacts);
        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(1, "Other", "+201012345678", Today, default));
        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(999, "Other", "+201012345678", Today, default));
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
    }
}
