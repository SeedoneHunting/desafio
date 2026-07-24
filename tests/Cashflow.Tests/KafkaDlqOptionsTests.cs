using Cashflow.Contracts;

namespace Cashflow.Tests;

public class KafkaDlqOptionsTests
{
    [Fact]
    public void KafkaOptions_DefaultsIncludeDlqTopic()
    {
        var options = new KafkaOptions();
        Assert.Equal("cashflow.entries", options.Topic);
        Assert.Equal("cashflow.entries.dlq", options.DlqTopic);
    }
}
