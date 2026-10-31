using System.ComponentModel.DataAnnotations;
using service_requests_service.Models;

namespace service_requests_service.Persistent.Entities;

public sealed class ServiceRequest
{
    [Key] [Required] public Guid Id { get; set; }
    [MaxLength(200)] [Required] public string Title { get; set; } = null!;
    [MaxLength(2000)] public string? Description { get; set; }
    [Required] public ServiceRequestPriority Priority { get; set; } = ServiceRequestPriority.Normal;
    [Required] public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.New;
    [Required] public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    //EF Core needs    
    public ServiceRequest() { }

    public ServiceRequest(string title, string? description, ServiceRequestPriority? priority)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        Priority = priority ?? ServiceRequestPriority.Normal;
        Status = ServiceRequestStatus.New;
        CreatedAt = DateTime.UtcNow;
    }
}