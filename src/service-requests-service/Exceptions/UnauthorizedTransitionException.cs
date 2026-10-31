using service_requests_service.Models;

namespace service_requests_service.Exceptions;

public class UnauthorizedTransitionException(ServiceRequestStatus oldStatus, ServiceRequestStatus newStatus)
    : Exception($"Cannot change status from {oldStatus} to {newStatus}.");