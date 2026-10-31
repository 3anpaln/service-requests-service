namespace service_requests_service.Models;

// For production maybe need to map to separate table into DB
public enum ServiceRequestPriority
{
    Low = 1,
    Normal = 2,
    High = 3
}