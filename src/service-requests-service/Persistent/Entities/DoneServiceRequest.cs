using System.ComponentModel.DataAnnotations;
using service_requests_service.Models;

namespace service_requests_service.Persistent.Entities;

// Not fully understand whu we need separate table for this
public sealed class DoneServiceRequest
{
    [Key] [Required] public Guid Id { get; set; }
    [MaxLength(200)] [Required] public string Title { get; set; } = null!;
    [MaxLength(2000)] public string? Description { get; set; }
    [Required] public ServiceRequestPriority Priority { get; set; }
    [Required] public ServiceRequestStatus Status { get; set; }
    [Required] public DateTime CreatedAt { get; set; }
}