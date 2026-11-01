namespace service_requests_service.Contracts;

public interface IRabbitMqConsumer<TMessage> where TMessage : IRabbitMqMessage
{
    public TMessage DeserializeMessage(byte[] message);
    public Task<bool> ConsumeAsync(TMessage message);
    public IExchangeInfo ExchangeInfo { get; }
    public IQueueInfo QueueInfo { get; }
}