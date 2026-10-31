namespace service_requests_service.Models;

public sealed record CreateServiceRequestRequest
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public ServiceRequestPriority? Priority { get; set; }
}