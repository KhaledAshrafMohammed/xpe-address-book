using System.Text.RegularExpressions;
using AddressBook.Core.Entities;

namespace AddressBook.Core.Validation;

public static partial class ContactRules
{
    [GeneratedRegex(@"\A\+[1-9][0-9]{7,14}\z", RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();

    public static int AgeOn(DateOnly birthDate, DateOnly today)
    {
        var age = today.Year - birthDate.Year;
        return birthDate.AddYears(age) > today ? age - 1 : age;
    }

    public static void Validate(Contact contact, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(contact.Name) || contact.Name.Length > 200 || contact.Name.Any(char.IsControl))
            throw new InputException("name", "Contact name is required and must be at most 200 characters without control characters.");
        if (!PhonePattern().IsMatch(contact.PhoneNumber))
            throw new InputException("phoneNumber", "Use international format: + followed by 8–15 digits, with no spaces (e.g. +201012345678).");
        if (contact.BirthDate == default || contact.BirthDate > today)
            throw new InputException("birthDate", "A birthday on or before today is required.");
    }

    public static void ValidateForCompany(Company company, int count, Contact contact, DateOnly today)
    {
        if (count >= company.NumContacts)
            throw new ConflictException("This company has reached its contact capacity.");
        if (company.MaxContactAge is int maxAge && AgeOn(contact.BirthDate, today) > maxAge)
            throw new InputException("birthDate", $"Contact age must be {maxAge} or younger.");
    }
}
