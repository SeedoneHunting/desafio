namespace Cashflow.Contracts;

public sealed record EntryResponse(
    Guid Id,
    EntryType Type,
    decimal Amount,
    DateOnly Date,
    string Description,
    DateTimeOffset CreatedAt);
