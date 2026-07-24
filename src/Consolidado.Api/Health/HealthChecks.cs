using Consolidado.Api.Data;
using Consolidado.Api.Workers;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Consolidado.Api.Health;

public sealed class DatabaseHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
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

public sealed class KafkaConsumerHealthCheck(KafkaConsumerWorker consumer) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (consumer.IsRunning)
        {
            return Task.FromResult(HealthCheckResult.Healthy(
                "Kafka consumer loop is running.",
                data: new Dictionary<string, object>
                {
                    ["processedEvents"] = consumer.ProcessedEvents
                }));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("Kafka consumer loop is not running."));
    }
}
