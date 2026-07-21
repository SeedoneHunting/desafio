namespace Cashflow.Contracts;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "cashflow.entries";
    public string ConsumerGroup { get; set; } = "consolidado-service";
}
