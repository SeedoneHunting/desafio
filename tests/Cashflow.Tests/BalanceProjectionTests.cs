extern alias ConsolidadoApi;
using Cashflow.Contracts;
using ConsolidadoApi::Consolidado.Api.Data;
using ConsolidadoApi::Consolidado.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Cashflow.Tests;

public class BalanceProjectionTests
{
    [Fact]
    public async Task ProcessEvent_IsIdempotent_ForSameEventId()
    {
        var options = new DbContextOptionsBuilder<ConsolidadoDbContext>()
            .UseSqlite($"Data Source={Path.GetTempFileName()}")
            .Options;

        await using var db = new ConsolidadoDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new BalanceProjectionService(db, cache);

        var date = new DateOnly(2026, 1, 25);
        var domainEvent = new EntryCreatedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            EntryType.Credit,
            50m,
            date,
            DateTimeOffset.UtcNow);

        await service.ProcessEventAsync(domainEvent, CancellationToken.None);
        await service.ProcessEventAsync(domainEvent, CancellationToken.None);

        var balance = await service.GetByDateAsync(date, CancellationToken.None);

        Assert.NotNull(balance);
        Assert.Equal(50m, balance.Balance);
        Assert.Equal(50m, balance.TotalCredits);
    }

    [Fact]
    public async Task ProcessEvent_ComputesDailyBalance_FromCreditsAndDebits()
    {
        var options = new DbContextOptionsBuilder<ConsolidadoDbContext>()
            .UseSqlite($"Data Source={Path.GetTempFileName()}")
            .Options;

        await using var db = new ConsolidadoDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new BalanceProjectionService(db, cache);
        var date = new DateOnly(2026, 1, 25);

        await service.ProcessEventAsync(new EntryCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), EntryType.Credit, 100m, date, DateTimeOffset.UtcNow),
            CancellationToken.None);

        await service.ProcessEventAsync(new EntryCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), EntryType.Debit, 25.50m, date, DateTimeOffset.UtcNow),
            CancellationToken.None);

        var balance = await service.GetByDateAsync(date, CancellationToken.None);

        Assert.NotNull(balance);
        Assert.Equal(74.50m, balance.Balance);
    }
}
