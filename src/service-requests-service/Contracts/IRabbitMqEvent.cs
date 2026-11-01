namespace service_requests_service.Contracts;

public interface IRabbitMqEvent<TMessage> where TMessage : IRabbitMqMessage
{
    byte[] Serialize(TMessage message);
    IExchangeInfo ExchangeInfo { get; }
    TMessage Body { get; }
}