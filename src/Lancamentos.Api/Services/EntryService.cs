using System.Text.Json;
using Cashflow.Contracts;
using Lancamentos.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Api.Services;

public sealed class EntryService(LancamentosDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<EntryResponse> CreateAsync(CreateEntryRequest request, CancellationToken ct)
    {
        Validate(request);

        var entryId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var entry = new EntryEntity
        {
            Id = entryId,
            Type = (int)request.Type,
            Amount = request.Amount,
            Date = request.Date,
            Description = request.Description.Trim(),
            CreatedAt = now
        };

        var domainEvent = new EntryCreatedEvent(
            eventId,
            entryId,
            request.Type,
            request.Amount,
            request.Date,
            now);

        var outbox = new OutboxMessageEntity
        {
            Id = eventId,
            Payload = JsonSerializer.Serialize(domainEvent, JsonOptions),
            CreatedAt = now
        };

        db.Entries.Add(entry);
        db.OutboxMessages.Add(outbox);
        await db.SaveChangesAsync(ct);

        return ToResponse(entry);
    }

    public async Task<IReadOnlyList<EntryResponse>> ListAsync(DateOnly? date, CancellationToken ct)
    {
        var query = db.Entries.AsNoTracking().AsQueryable();

        if (date is not null)
            query = query.Where(e => e.Date == date);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

        return items.Select(ToResponse).ToList();
    }

    public Task<int> CountPendingOutboxAsync(CancellationToken ct) =>
        db.OutboxMessages.CountAsync(m => m.ProcessedAt == null, ct);

    private static void Validate(CreateEntryRequest request)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(request.Description))
            throw new ArgumentException("Description is required.");
    }

    private static EntryResponse ToResponse(EntryEntity entry) =>
        new(
            entry.Id,
            (EntryType)entry.Type,
            entry.Amount,
            entry.Date,
            entry.Description,
            entry.CreatedAt);
}
