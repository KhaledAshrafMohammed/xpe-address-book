namespace AddressBook.Api.Contracts.Companies;

public sealed record RejectedRowResponse(int Line, string Reason);
public sealed record ImportResponse(int TotalRows, int ImportedRows, int SkippedDuplicates, int RejectedCount, IReadOnlyList<RejectedRowResponse> RejectedRows);
