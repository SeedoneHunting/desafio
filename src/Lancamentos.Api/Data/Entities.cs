namespace Lancamentos.Api.Data;

public sealed class EntryEntity
{
    public Guid Id { get; set; }
    public Guid ExternalId { get; set; }
    public int Type { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class OutboxMessageEntity
{
    public Guid Id { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
