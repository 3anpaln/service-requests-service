namespace service_requests_service.Contracts;

public interface IQueueInfo
{
    public string QueueName { get; }
    public bool IsDurable { get; }
    public bool IsAutoAck { get; }
    public bool IsMultiple { get; }
    public bool IsExclusive { get; }
    public bool IsAutoDelete { get; }
    public bool IsNackRequeue { get; }
}