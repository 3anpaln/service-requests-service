using System.ComponentModel.DataAnnotations;

namespace service_requests_service.Persistent.Entities;

//For production nice to have more generic tables and entities
public sealed class InboxEvent
{
    [Key] [Required] public Guid IdempotencyKey { get; set; }
    [Required] [MaxLength(400)] public string ConsumerName { get; set; } = null!;
    [Required] public DateTime ProcessedAt { get; set; }
}