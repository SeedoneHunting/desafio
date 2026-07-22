using Cashflow.Contracts;
using Consolidado.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Consolidado.Api.Services;

public sealed class BalanceProjectionService(
    ConsolidadoDbContext db,
    IMemoryCache cache)
{
    private static string CacheKey(DateOnly date) => $"balance:{date:yyyy-MM-dd}";

    public async Task ProcessEventAsync(EntryCreatedEvent domainEvent, CancellationToken ct)
    {
        var alreadyProcessed = await db.ProcessedEvents
            .AnyAsync(e => e.EventId == domainEvent.EventId, ct);

        if (alreadyProcessed)
            return;

        var balance = await db.DailyBalances
            .FirstOrDefaultAsync(b => b.Date == domainEvent.Date, ct);

        if (balance is null)
        {
            balance = new DailyBalanceEntity
            {
                Date = domainEvent.Date,
                TotalCredits = 0,
                TotalDebits = 0,
                Balance = 0,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.DailyBalances.Add(balance);
        }

        if (domainEvent.Type == EntryType.Credit)
        {
            balance.TotalCredits += domainEvent.Amount;
            balance.Balance += domainEvent.Amount;
        }
        else
        {
            balance.TotalDebits += domainEvent.Amount;
            balance.Balance -= domainEvent.Amount;
        }

        balance.UpdatedAt = DateTimeOffset.UtcNow;

        db.ProcessedEvents.Add(new ProcessedEventEntity
        {
            EventId = domainEvent.EventId,
            ProcessedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
        cache.Remove(CacheKey(domainEvent.Date));
    }

    public async Task<DailyBalanceResponse?> GetByDateAsync(DateOnly date, CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey(date), out DailyBalanceResponse? cached) && cached is not null)
            return cached;

        var balance = await db.DailyBalances.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Date == date, ct);

        if (balance is null)
            return null;

        var response = new DailyBalanceResponse(
            balance.Date,
            balance.TotalCredits,
            balance.TotalDebits,
            balance.Balance);

        cache.Set(CacheKey(date), response, TimeSpan.FromMinutes(5));
        return response;
    }

    public async Task<IReadOnlyList<DailyBalanceResponse>> ListAsync(
        DateOnly? startDate,
        DateOnly? endDate,
        CancellationToken ct)
    {
        var query = db.DailyBalances.AsNoTracking().AsQueryable();

        if (startDate is not null)
            query = query.Where(b => b.Date >= startDate);

        if (endDate is not null)
            query = query.Where(b => b.Date <= endDate);

        var items = await query.OrderBy(b => b.Date).ToListAsync(ct);

        return items.Select(b => new DailyBalanceResponse(
            b.Date,
            b.TotalCredits,
            b.TotalDebits,
            b.Balance)).ToList();
    }
}
