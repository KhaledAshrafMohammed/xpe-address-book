using AddressBook.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressBook.Infrastructure.Data.Configurations;

public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts", table =>
        {
            table.HasCheckConstraint("CK_Contacts_Name", "LEN(LTRIM(RTRIM([Name]))) > 0");
            table.HasCheckConstraint("CK_Contacts_PhoneNumber", "LEN([PhoneNumber]) BETWEEN 9 AND 16 AND LEFT([PhoneNumber], 1) = '+' AND SUBSTRING([PhoneNumber], 2, 1) COLLATE Latin1_General_100_BIN2 LIKE '[1-9]' AND SUBSTRING([PhoneNumber], 2, 15) COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'");
        });
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.PhoneNumber).HasMaxLength(16).IsRequired().IsUnicode(false);
        builder.Property(c => c.BirthDate).HasColumnType("date").IsRequired();
    }
}
