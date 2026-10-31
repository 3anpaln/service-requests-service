using service_requests_service.Models;
using service_requests_service.Persistent.Entities;

namespace service_requests_service.Contracts;

public interface IOutboxService
{
    public Task StoreEventWithoutSaveChangesAsync<TEvent>(Guid idempotencyKey, EventType eventType, TEvent e,
        CancellationToken ct = default);

    public Task<List<OutboxEvent>> RetrieveUnsendEventsAsync(EventType eventType,
        CancellationToken ct = default);

    public Task MarkAsSendAsync(Guid idempotencyKey, CancellationToken ct = default);
}