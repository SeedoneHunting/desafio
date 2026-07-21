using Lancamentos.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Api.Data;

public sealed class LancamentosDbContext(DbContextOptions<LancamentosDbContext> options) : DbContext(options)
{
    public DbSet<EntryEntity> Entries => Set<EntryEntity>();
    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EntryEntity>(entity =>
        {
            entity.ToTable("entries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Date);
        });

        modelBuilder.Entity<OutboxMessageEntity>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProcessedAt);
        });
    }
}
