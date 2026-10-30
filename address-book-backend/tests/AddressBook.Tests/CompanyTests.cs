using AddressBook.Core.Entities;
using AddressBook.Core.Services;
using AddressBook.Core.Validation;
using Xunit;

namespace AddressBook.Tests;

public sealed class CompanyTests
{
    [Theory]
    [InlineData("ExpertPeople2®")]
    [InlineData("XI9™V")]
    [InlineData("™جدير1®")]
    [InlineData("سمير7ʬ7وعادل")]
    [InlineData("D2ʬLL")]
    [InlineData("1§")]
    [InlineData("2©")]
    [InlineData("3@")]
    [InlineData("🚀4®")]
    public void Accepts_supported_name_patterns(string name) => Assert.True(CompanyRules.IsValidName(name));

    [Theory]
    [InlineData("ExpertsPeople®2")]
    [InlineData("©Zen1∞ox")]
    [InlineData("Hb")]
    [InlineData("Abb₠le")]
    [InlineData("أولاد©عمر")]
    [InlineData("الحسين6للألعاب")]
    [InlineData("D2₠LL")]
    [InlineData("٢®")]
    [InlineData("２®")]
    [InlineData("1 ®")]
    [InlineData("1\n®")]
    [InlineData("A1®\u0000")]
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_invalid_names_and_non_ascii_digits(string? name) => Assert.False(CompanyRules.IsValidName(name));

    [Fact]
    public void Name_length_is_enforced() => Assert.False(CompanyRules.IsValidName(new string('a', 199) + "1®"));

    [Fact]
    public async Task Manual_creation_trims_names_and_rejects_existing_name()
    {
        var service = new CompanyService(new MemoryStore());
        var company = await service.CreateAsync("  New1®  ", 0, null, default);
        Assert.Equal("New1®", company.Name);
        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync("New1®", 2, 40, default));
    }

    [Theory]
    [InlineData(-1, null)]
    [InlineData(1, -1)]
    public void Negative_limits_are_invalid(int capacity, int? age) => Assert.Throws<InputException>(() => CompanyRules.Validate("A1®", capacity, age));

    [Fact]
    public async Task Actual_sample_imports_five_rows_and_is_repeatable()
    {
        var store = new MemoryStore();
        var service = new CompanyService(store);
        using var reader = File.OpenText(Path.Combine(AppContext.BaseDirectory, "SampleData.txt"));
        var result = await service.ImportAsync(reader, default);
        Assert.Equal(12, result.TotalRows);
        Assert.Equal(5, result.ImportedRows);
        Assert.Equal(7, result.RejectedRows.Count);
        Assert.Equal(0, result.SkippedDuplicates);
        Assert.Equal(0, store.Companies.Single(c => c.Name == "D2ʬLL").NumContacts);
        Assert.Null(store.Companies.Single(c => c.Name == "XI9™V").MaxContactAge);
        using var again = File.OpenText(Path.Combine(AppContext.BaseDirectory, "SampleData.txt"));
        var repeated = await service.ImportAsync(again, default);
        Assert.Equal(0, repeated.ImportedRows);
        Assert.Equal(5, repeated.SkippedDuplicates);
        Assert.Equal(7, repeated.RejectedRows.Count);
    }

    [Fact]
    public async Task Import_preserves_empty_age_and_does_not_update_duplicates()
    {
        var store = new MemoryStore();
        var service = new CompanyService(store);
        await service.CreateAsync("Existing1®", 5, 20, default);
        using var reader = Txt("Existing1®\t\t99\t\t99\nFresh2@\t\t0\t\t\nFresh2@\t\t10\t\t10\nfresh2@\t\t1\t\t3");
        var result = await service.ImportAsync(reader, default);
        Assert.Equal(4, result.TotalRows);
        Assert.Equal(2, result.ImportedRows);
        Assert.Equal(2, result.SkippedDuplicates);
        Assert.Empty(result.RejectedRows);
        Assert.Equal(5, store.Companies.Single(c => c.Name == "Existing1®").NumContacts);
        Assert.Null(store.Companies.Single(c => c.Name == "Fresh2@").MaxContactAge);
    }

    [Theory]
    [InlineData("A1®\t\t-1\t\t20")]
    [InlineData("A1®\t\tx\t\t20")]
    [InlineData("A1®\t\t2.5\t\t20")]
    [InlineData("A1®\t\t2147483648\t\t20")]
    [InlineData("A1®\t\t2\t\t-2")]
    [InlineData("A1®\t\t2\t\tnan")]
    [InlineData("A1®\t\t2")]
    [InlineData("A1®\t2\t20")]
    [InlineData("A1®\t\t2\t\t20\t\textra")]
    public async Task Malformed_row_does_not_stop_valid_rows(string badRow)
    {
        using var reader = Txt(badRow + "\nGood2™\t\t3\t\t");
        var result = await new CompanyService(new MemoryStore()).ImportAsync(reader, default);
        Assert.Equal(2, result.TotalRows); Assert.Equal(1, result.ImportedRows);
        Assert.Equal(2, Assert.Single(result.RejectedRows).Line);
    }

    [Fact]
    public async Task Invalid_header_and_row_limit_fail_before_any_insert()
    {
        var store = new MemoryStore(); var service = new CompanyService(store);
        await Assert.ThrowsAsync<InputException>(() => service.ImportAsync(new StringReader("bad header"), default));
        using var reader = Txt(string.Join('\n', Enumerable.Repeat("A1®\t\t1\t\t", 2001)));
        await Assert.ThrowsAsync<InputException>(() => service.ImportAsync(reader, default));
        Assert.Empty(store.Companies);
    }

    private static StringReader Txt(string rows) => new("\uFEFF#Company\t\t#NumContacts\t\t#MaxContactAge\n" + rows + "\n");
}
