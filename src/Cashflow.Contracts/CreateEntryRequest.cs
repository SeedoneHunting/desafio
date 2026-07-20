namespace Cashflow.Contracts;

public sealed record CreateEntryRequest(
    EntryType Type,
    decimal Amount,
    DateOnly Date,
    string Description);
