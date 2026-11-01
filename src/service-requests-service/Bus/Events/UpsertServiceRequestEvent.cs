using service_requests_service.Bus.Common;
using service_requests_service.Bus.Messages;
using service_requests_service.Contracts;
using service_requests_service.Services;

namespace service_requests_service.Bus.Events;

public sealed class UpsertServiceRequestEvent : IRabbitMqEvent<UpsertServiceRequestMessage>
{
    public UpsertServiceRequestEvent(UpsertServiceRequestMessage body)
    {
        Body = body;
    }

    public byte[] Serialize(UpsertServiceRequestMessage message) => JsonSerializer.JsonSerializeMessage(message);

    public IExchangeInfo ExchangeInfo => new UpsertServiceRequestExchangeInfo();
    public UpsertServiceRequestMessage Body { get; }
}