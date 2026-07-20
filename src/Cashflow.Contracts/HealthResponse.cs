namespace Cashflow.Contracts;

public sealed record HealthResponse(
    string Status,
    int PendingOutboxCount);
