namespace AddressBook.Core.Validation;

public sealed class InputException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}

public sealed class NotFoundException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
