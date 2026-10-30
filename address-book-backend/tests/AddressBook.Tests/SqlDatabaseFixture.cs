using AddressBook.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AddressBook.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION")))
            Skip = "Set SQLSERVER_TEST_CONNECTION to run SQL Server integration tests against a disposable database.";
    }
}

[CollectionDefinition("SqlServer")]
public sealed class SqlServerCollection : ICollectionFixture<SqlDatabaseFixture>;

public sealed class SqlDatabaseFixture : IAsyncLifetime
{
    private readonly string databaseName = "AddressBook_Test_" + Guid.NewGuid().ToString("N");
    private bool created;
    public string ConnectionString { get; private set; } = "";

    public AddressBookDbContext CreateContext() => new(new DbContextOptionsBuilder<AddressBookDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        var source = Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(source)) return;
        ConnectionString = new SqlConnectionStringBuilder(source) { InitialCatalog = databaseName }.ConnectionString;
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        created = true;
    }

    public async Task DisposeAsync()
    {
        if (!created) return;
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }
}
