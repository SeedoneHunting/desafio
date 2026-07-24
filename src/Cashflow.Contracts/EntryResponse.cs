namespace Cashflow.Contracts;

public sealed record EntryResponse(
    Guid Id,
    Guid ExternalId,
    EntryType Type,
    decimal Amount,
    DateOnly Date,
    string Description,
    DateTimeOffset CreatedAt);
