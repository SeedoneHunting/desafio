using Cashflow.Contracts;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Lancamentos.Api.Messaging;

public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;

    public KafkaEventPublisher(IOptions<KafkaOptions> options)
    {
        var kafka = options.Value;
        _topic = kafka.Topic;
        var config = new ProducerConfig
        {
            BootstrapServers = kafka.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(string key, string payload, CancellationToken cancellationToken = default)
    {
        await _producer.ProduceAsync(
            _topic,
            new Message<string, string> { Key = key, Value = payload },
            cancellationToken);
    }

    public void Dispose() => _producer.Dispose();
}
