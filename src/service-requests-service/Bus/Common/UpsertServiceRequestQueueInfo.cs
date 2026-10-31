using service_requests_service.Contracts;

namespace service_requests_service.Bus.Common;

public sealed class UpsertServiceRequestQueueInfo : IQueueInfo
{
    public string QueueName => "sr.events";
    public bool IsDurable => true;
    public bool IsAutoAck => false;
    public bool IsMultiple => false;
    public bool IsExclusive => false;
    public bool IsAutoDelete => false;
    //For production configured DLQ for non eternity issues
    public bool IsNackRequeue => true;
}