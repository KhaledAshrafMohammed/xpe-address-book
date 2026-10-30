using AddressBook.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AddressBook.Infrastructure.Data;

public sealed class AddressBookDbContext(DbContextOptions<AddressBookDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Contact> Contacts => Set<Contact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AddressBookDbContext).Assembly);
}
