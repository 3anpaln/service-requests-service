namespace service_requests_service.Models;

// For production map to separate table into DB
public enum ServiceRequestStatus
{
    New = 1,
    InProgress = 2,
    Done = 3,
    Cancelled = 4
}