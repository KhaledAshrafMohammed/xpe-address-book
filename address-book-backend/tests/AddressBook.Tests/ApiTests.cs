using System.Net;
using System.Net.Http.Json;
using System.Text;
using AddressBook.Api.Contracts.Companies;
using AddressBook.Api.Contracts.Contacts;
using AddressBook.Infrastructure.Data;
using AddressBook.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace AddressBook.Tests;

[Collection("SqlServer")]
public sealed class ApiTests(SqlDatabaseFixture fixture)
{
    private WebApplicationFactory<Program> Factory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AddressBookDbContext>();
            services.RemoveAll<DbContextOptions<AddressBookDbContext>>();
            services.AddDbContext<AddressBookDbContext>(options => options.UseSqlServer(fixture.ConnectionString));
        });
    });

    [SqlServerFact]
    public async Task Api_creates_company_validates_contacts_and_enforces_capacity()
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var company = new { name = "Api1®" + Guid.NewGuid().ToString("N"), numContacts = 1, maxContactAge = 30 };
        var created = await client.PostAsJsonAsync("/api/companies", company);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var result = (await created.Content.ReadFromJsonAsync<CompanyResponse>())!;
        Assert.NotNull(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(created.Headers.Location)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/companies", company)).StatusCode);
        var path = $"/api/companies/{result.Id}/contacts";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path, new { name = "C", phoneNumber = "bad", birthDate = today })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path, new { name = "C", phoneNumber = "+201012345678", birthDate = today.AddDays(1) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path, new { name = "C", phoneNumber = "+201012345678", birthDate = today.AddYears(-31) })).StatusCode);
        var valid = new { name = "Contact", phoneNumber = "+201012345678", birthDate = today.AddYears(-30) };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(path, valid)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path, valid)).StatusCode);
        Assert.Single((await client.GetFromJsonAsync<ContactResponse[]>(path))!);
        Assert.Equal(1, (await client.GetFromJsonAsync<CompanyResponse>($"/api/companies/{result.Id}"))!.ContactCount);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/companies/2147483647")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/companies/2147483647/contacts")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/companies/2147483647/contacts", valid)).StatusCode);
    }

    [SqlServerFact]
    public async Task Actual_sample_imports_over_http_and_repeated_upload_skips_duplicates()
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        var bytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "SampleData.txt"));
        using var first = Upload(bytes); var response = await client.PostAsync("/api/companies/import", first);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = (await response.Content.ReadFromJsonAsync<ImportResponse>())!;
        Assert.Equal(12, summary.TotalRows); Assert.Equal(5, summary.ImportedRows); Assert.Equal(7, summary.RejectedCount);
        using var second = Upload(bytes); var repeated = await client.PostAsync("/api/companies/import", second);
        var again = (await repeated.Content.ReadFromJsonAsync<ImportResponse>())!;
        Assert.Equal(0, again.ImportedRows); Assert.Equal(5, again.SkippedDuplicates);
        var companies = (await client.GetFromJsonAsync<CompanyResponse[]>("/api/companies"))!;
        Assert.Contains(companies, c => c.Name == "™جدير1®");
        var zero = companies.Single(c => c.Name == "D2ʬLL");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/companies/{zero.Id}/contacts", new { name = "C", phoneNumber = "+201012345678", birthDate = "2000-01-01" })).StatusCode);
    }

    [SqlServerFact]
    public async Task Invalid_requests_return_problem_details_instead_of_server_errors()
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        foreach (var body in new object[] { new { name = "Invalid", numContacts = 1 }, new { name = "A1®" }, new { name = "A1®", numContacts = -1 } })
        {
            var response = await client.PostAsJsonAsync("/api/companies", body);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
        using var wrongExtension = Upload(Encoding.UTF8.GetBytes("test"), "bad.csv");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/companies/import", wrongExtension)).StatusCode);
        using var badUtf8 = Upload([0xff, 0xfe, 0xff]);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/companies/import", badUtf8)).StatusCode);
        using var empty = Upload([]);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/companies/import", empty)).StatusCode);
        using var oversized = Upload(new byte[1024 * 1024 + 1]);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/companies/import", oversized)).StatusCode);
    }

    private static MultipartFormDataContent Upload(byte[] bytes, string name = "companies.txt")
    {
        var body = new MultipartFormDataContent(); body.Add(new ByteArrayContent(bytes), "file", name); return body;
    }

    [SqlServerFact]
    public async Task Browser_accept_header_still_receives_clear_problem_details()
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        var response = await client.GetAsync("/api/companies/2147483647");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Company not found.", await response.Content.ReadAsStringAsync());
        var invalid = await client.PostAsJsonAsync("/api/companies", new { name = "Invalid", numContacts = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Contains("errors", await invalid.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Unexpected_server_failures_do_not_expose_internal_details()
    {
        await using var factory = Factory().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddScoped<IAddressBookStore>(_ => throw new InvalidOperationException("Private internal diagnostic"))));
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/companies");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("traceId", body);
        Assert.DoesNotContain("Private internal diagnostic", body);
        Assert.DoesNotContain("stack", body, StringComparison.OrdinalIgnoreCase);
    }
}
