using System.Text.Json;
using Cashflow.Contracts;
using Consolidado.Api.Services;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Consolidado.Api.Workers;

public sealed class KafkaConsumerWorker(
    IServiceProvider services,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<KafkaConsumerWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var kafka = kafkaOptions.Value;
        var config = new ConsumerConfig
        {
            BootstrapServers = kafka.BootstrapServers,
            GroupId = kafka.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(kafka.Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                if (result?.Message?.Value is null)
                    continue;

                var domainEvent = JsonSerializer.Deserialize<EntryCreatedEvent>(result.Message.Value, JsonOptions);
                if (domainEvent is null)
                {
                    consumer.Commit(result);
                    continue;
                }

                using var scope = services.CreateScope();
                var projection = scope.ServiceProvider.GetRequiredService<BalanceProjectionService>();
                await projection.ProcessEventAsync(domainEvent, stoppingToken);
                consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                logger.LogWarning(ex, "Kafka consume error.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed processing Kafka message.");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}
