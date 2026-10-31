namespace service_requests_service.Contracts;

public interface IExchangeInfo
{
    public string ExchangeName { get; }
    public string ExchangeType { get; }
    public string RoutingKey { get; }
}