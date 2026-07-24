extern alias ConsolidadoApi;
using Cashflow.Contracts;
using ConsolidadoApi::Consolidado.Api.Data;
using ConsolidadoApi::Consolidado.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cashflow.Tests;

public class BalanceProjectionTests
{
    private static BalanceProjectionService CreateService(ConsolidadoDbContext db) =>
        new(db, new MemoryCache(new MemoryCacheOptions()), NullLogger<BalanceProjectionService>.Instance);

    [Fact]
    public async Task ProcessEvent_IsIdempotent_ForSameEventId()
    {
        var options = new DbContextOptionsBuilder<ConsolidadoDbContext>()
            .UseSqlite($"Data Source={Path.GetTempFileName()}")
            .Options;

        await using var db = new ConsolidadoDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var service = CreateService(db);

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
        Assert.Equal(1, await db.ProcessedEvents.CountAsync());
    }

    [Fact]
    public async Task ProcessEvent_ComputesDailyBalance_FromCreditsAndDebits()
    {
        var options = new DbContextOptionsBuilder<ConsolidadoDbContext>()
            .UseSqlite($"Data Source={Path.GetTempFileName()}")
            .Options;

        await using var db = new ConsolidadoDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var service = CreateService(db);
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

    [Fact]
    public async Task ProcessedEvents_HasUniqueEventId()
    {
        var options = new DbContextOptionsBuilder<ConsolidadoDbContext>()
            .UseSqlite($"Data Source={Path.GetTempFileName()}")
            .Options;

        await using var db = new ConsolidadoDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var eventId = Guid.NewGuid();
        db.ProcessedEvents.Add(new ProcessedEventEntity
        {
            EventId = eventId,
            ProcessedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        db.ProcessedEvents.Add(new ProcessedEventEntity
        {
            EventId = eventId,
            ProcessedAt = DateTimeOffset.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
