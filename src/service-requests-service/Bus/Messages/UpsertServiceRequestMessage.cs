using service_requests_service.Contracts;
using service_requests_service.Models;

namespace service_requests_service.Bus.Messages;

public class UpsertServiceRequestMessage : IRabbitMqMessage
{
    public UpsertServiceRequestMessage(
        Guid serviceRequestId,
        ServiceRequestStatus? oldStatus,
        ServiceRequestStatus newStatus,
        ServiceRequestPriority priority
    )
    {
        IdempotencyKey = Guid.NewGuid();
        ServiceRequestId = serviceRequestId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Priority = priority;
    }

    public Guid ServiceRequestId { get; set; }
    public ServiceRequestStatus? OldStatus { get; set; }
    public ServiceRequestStatus NewStatus { get; set; }
    public ServiceRequestPriority Priority { get; set; }

    //Probably can be auto property for production reasons 
    public Guid IdempotencyKey { get; set; }
}