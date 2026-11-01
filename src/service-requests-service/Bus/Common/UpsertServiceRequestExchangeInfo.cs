using service_requests_service.Contracts;

namespace service_requests_service.Bus.Common;

public sealed class UpsertServiceRequestExchangeInfo : IExchangeInfo
{
    public string ExchangeName => "sr.topic";
    public string ExchangeType => "topic";
    public string RoutingKey => "sr.upsert";
}