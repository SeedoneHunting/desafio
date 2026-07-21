using System.Text.Json;
using Lancamentos.Api.Data;
using Lancamentos.Api.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Api.Services;

public sealed class OutboxRelayWorker(
    IServiceProvider services,
    IEventPublisher eventPublisher,
    IConfiguration configuration,
    ILogger<OutboxRelayWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = configuration.GetValue("Outbox:PollIntervalSeconds", 2);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Outbox relay iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        if (pending.Count == 0)
            return;

        foreach (var message in pending)
        {
            try
            {
                await eventPublisher.PublishAsync(message.Id.ToString(), message.Payload, ct);
                message.ProcessedAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogDebug(ex, "Kafka publish failed for outbox message {MessageId}.", message.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
