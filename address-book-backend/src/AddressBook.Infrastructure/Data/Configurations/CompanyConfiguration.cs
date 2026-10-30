using AddressBook.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressBook.Infrastructure.Data.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies", table =>
        {
            table.HasCheckConstraint("CK_Companies_NumContacts", "[NumContacts] >= 0");
            table.HasCheckConstraint("CK_Companies_MaxContactAge", "[MaxContactAge] IS NULL OR [MaxContactAge] >= 0");
            table.HasCheckConstraint("CK_Companies_Name", "LEN(LTRIM(RTRIM([Name]))) > 0");
        });
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired().UseCollation("Latin1_General_100_BIN2");
        builder.HasIndex(c => c.Name).IsUnique();
        builder.HasMany(c => c.Contacts).WithOne(c => c.Company).HasForeignKey(c => c.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}
