namespace Consolidado.Api.Data;

public sealed class ProcessedEventEntity
{
    public Guid EventId { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}

public sealed class DailyBalanceEntity
{
    public DateOnly Date { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal Balance { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
