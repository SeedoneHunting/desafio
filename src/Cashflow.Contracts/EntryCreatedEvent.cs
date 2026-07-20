namespace Cashflow.Contracts;

public sealed record EntryCreatedEvent(
    Guid EventId,
    Guid EntryId,
    EntryType Type,
    decimal Amount,
    DateOnly Date,
    DateTimeOffset OccurredAt);
