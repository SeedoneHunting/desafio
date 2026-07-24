using System.Text.Json;
using Cashflow.Contracts;
using Consolidado.Api.Services;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Consolidado.Api.Workers;

public sealed class KafkaConsumerWorker : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly int[] RetryDelaysSeconds = [1, 2, 4];

    private readonly IServiceProvider _services;
    private readonly KafkaOptions _kafka;
    private readonly ILogger<KafkaConsumerWorker> _logger;
    private long _processedEvents;

    public KafkaConsumerWorker(
        IServiceProvider services,
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<KafkaConsumerWorker> logger)
    {
        _services = services;
        _kafka = kafkaOptions.Value;
        _logger = logger;
    }

    public bool IsRunning { get; private set; }
    public long ProcessedEvents => Interlocked.Read(ref _processedEvents);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafka.BootstrapServers,
            GroupId = _kafka.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafka.BootstrapServers,
            Acks = Acks.All
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        using var dlqProducer = new ProducerBuilder<string, string>(producerConfig).Build();

        consumer.Subscribe(_kafka.Topic);
        IsRunning = true;
        _logger.LogInformation("Kafka consumer subscribed to {Topic}.", _kafka.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(1));
                    if (result?.Message?.Value is null)
                        continue;

                    await ProcessWithRetryAsync(result, dlqProducer, stoppingToken);
                    consumer.Commit(result);
                }
                catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    _logger.LogWarning("Topic {Topic} not ready yet, retrying...", _kafka.Topic);
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogWarning(ex, "Kafka consume error.");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in Kafka consumer loop.");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
        }
        finally
        {
            IsRunning = false;
            consumer.Close();
        }
    }

    private async Task ProcessWithRetryAsync(
        ConsumeResult<string, string> result,
        IProducer<string, string> dlqProducer,
        CancellationToken stoppingToken)
    {
        Exception? lastError = null;

        for (var attempt = 0; attempt < RetryDelaysSeconds.Length; attempt++)
        {
            try
            {
                var domainEvent = JsonSerializer.Deserialize<EntryCreatedEvent>(result.Message.Value, JsonOptions);
                if (domainEvent is null)
                {
                    _logger.LogWarning(
                        "Invalid payload on {Topic} offset {Offset}: {Payload}",
                        _kafka.Topic,
                        result.Offset.Value,
                        result.Message.Value);

                    await PublishToDlqAsync(dlqProducer, result, "deserialization_failed", stoppingToken);
                    return;
                }

                using var scope = _services.CreateScope();
                var projection = scope.ServiceProvider.GetRequiredService<BalanceProjectionService>();
                await projection.ProcessEventAsync(domainEvent, stoppingToken);
                Interlocked.Increment(ref _processedEvents);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex;
                var delay = TimeSpan.FromSeconds(RetryDelaysSeconds[attempt]);
                _logger.LogWarning(
                    ex,
                    "Processing failed for offset {Offset} (attempt {Attempt}/{Max}). Retrying in {DelaySeconds}s. Payload: {Payload}",
                    result.Offset.Value,
                    attempt + 1,
                    RetryDelaysSeconds.Length,
                    delay.TotalSeconds,
                    result.Message.Value);

                await Task.Delay(delay, stoppingToken);
            }
        }

        _logger.LogError(
            lastError,
            "Permanent failure for offset {Offset}. Publishing to DLQ {DlqTopic}. Payload: {Payload}",
            result.Offset.Value,
            _kafka.DlqTopic,
            result.Message.Value);

        await PublishToDlqAsync(
            dlqProducer,
            result,
            lastError?.Message ?? "unknown_error",
            stoppingToken);
    }

    private async Task PublishToDlqAsync(
        IProducer<string, string> dlqProducer,
        ConsumeResult<string, string> result,
        string reason,
        CancellationToken cancellationToken)
    {
        var envelope = JsonSerializer.Serialize(new
        {
            reason,
            originalTopic = result.Topic,
            partition = result.Partition.Value,
            offset = result.Offset.Value,
            key = result.Message.Key,
            payload = result.Message.Value,
            failedAt = DateTimeOffset.UtcNow
        }, JsonOptions);

        await dlqProducer.ProduceAsync(
            _kafka.DlqTopic,
            new Message<string, string>
            {
                Key = result.Message.Key ?? result.Offset.Value.ToString(),
                Value = envelope
            },
            cancellationToken);
    }
}
