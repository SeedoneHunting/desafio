namespace Lancamentos.Api.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(string key, string payload, CancellationToken cancellationToken = default);
}
