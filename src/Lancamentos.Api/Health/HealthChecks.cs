using Lancamentos.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lancamentos.Api.Health;

public sealed class DatabaseHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Database reachable.")
                : HealthCheckResult.Unhealthy("Database unreachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database check failed.", ex);
        }
    }
}

public sealed class OutboxBacklogHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public const int DegradedThreshold = 100;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
        var pending = await db.OutboxMessages.CountAsync(m => m.ProcessedAt == null, cancellationToken);
        var data = new Dictionary<string, object> { ["pending"] = pending };

        if (pending > DegradedThreshold)
        {
            return HealthCheckResult.Degraded(
                $"Outbox backlog is {pending} (threshold {DegradedThreshold}).",
                data: data);
        }

        return HealthCheckResult.Healthy($"Outbox pending: {pending}.", data);
    }
}
