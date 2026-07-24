using Consolidado.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Consolidado.Api.Data;

public sealed class ConsolidadoDbContext(DbContextOptions<ConsolidadoDbContext> options) : DbContext(options)
{
    public DbSet<ProcessedEventEntity> ProcessedEvents => Set<ProcessedEventEntity>();
    public DbSet<DailyBalanceEntity> DailyBalances => Set<DailyBalanceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedEventEntity>(entity =>
        {
            entity.ToTable("processed_events");
            // Primary key enforces UNIQUE(event_id) for consumer idempotency.
            entity.HasKey(e => e.EventId);
        });

        modelBuilder.Entity<DailyBalanceEntity>(entity =>
        {
            entity.ToTable("daily_balances");
            entity.HasKey(e => e.Date);
            entity.Property(e => e.TotalCredits).HasPrecision(18, 2);
            entity.Property(e => e.TotalDebits).HasPrecision(18, 2);
            entity.Property(e => e.Balance).HasPrecision(18, 2);
        });
    }
}
