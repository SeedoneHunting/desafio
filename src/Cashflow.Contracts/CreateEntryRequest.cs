namespace Cashflow.Contracts;

public sealed record CreateEntryRequest(
    Guid ExternalId,
    EntryType Type,
    decimal Amount,
    DateOnly Date,
    string Description);
