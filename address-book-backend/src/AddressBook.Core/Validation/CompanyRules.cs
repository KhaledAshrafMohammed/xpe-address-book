using System.Text.RegularExpressions;

namespace AddressBook.Core.Validation;

public static partial class CompanyRules
{
    public const int MaxNameLength = 200;

    [GeneratedRegex(@"[0-9][§®™©ʬ@]", RegexOptions.CultureInvariant)]
    private static partial Regex ValidNamePattern();

    public static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= MaxNameLength &&
        !name.Any(char.IsControl) && ValidNamePattern().IsMatch(name);

    public static void Validate(string? name, int numContacts, int? maxContactAge)
    {
        if (!IsValidName(name))
            throw new InputException("name", "Use 1–200 characters including an ASCII digit immediately followed by §, ®, ™, ©, ʬ or @.");
        if (numContacts < 0)
            throw new InputException("numContacts", "Contact capacity must be a non-negative integer.");
        if (maxContactAge < 0)
            throw new InputException("maxContactAge", "Maximum age must be a non-negative integer or empty.");
    }
}
