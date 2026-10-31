using System.ComponentModel.DataAnnotations;
using service_requests_service.Models;

namespace service_requests_service.Persistent.Entities;

public sealed class OutboxEvent
{
    [Key] [Required] public Guid IdempotencyKey { get; set; }
    [Required] public EventType EventType { get; set; }
    [Required] public byte[] Payload { get; set; } = null!;
    [Required] public DateTime OccuredAt { get; set; }
    [Required] public bool IsSend { get; set; }
}