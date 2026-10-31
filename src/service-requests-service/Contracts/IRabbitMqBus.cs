namespace service_requests_service.Contracts;

public interface IRabbitMqBus
{
    public Task StartAsync(Func<IRabbitMqBus, Task> madeSubscriptions, CancellationToken ct = default);

    public Task SubscribeAsync<TConsumer, TMessage>(TConsumer consumerInstance, CancellationToken ct = default)
        where TConsumer : IRabbitMqConsumer<TMessage>
        where TMessage : IRabbitMqMessage;

    public Task PublishAsync<TMessage>(IRabbitMqEvent<TMessage> message, CancellationToken ct = default)
        where TMessage : IRabbitMqMessage;
}