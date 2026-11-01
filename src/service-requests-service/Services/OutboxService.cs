using Microsoft.EntityFrameworkCore;
using service_requests_service.Contracts;
using service_requests_service.Models;
using service_requests_service.Persistent.Data;
using service_requests_service.Persistent.Entities;

namespace service_requests_service.Services;

//For production maybe need to create repositories
public class OutboxService : IOutboxService
{
    private readonly AppDbContext _dbContext;

    public OutboxService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //AppDbContext must be registered as scoped!
    public async Task StoreEventWithoutSaveChangesAsync<TEvent>(Guid idempotencyKey, EventType eventType, TEvent e,
        CancellationToken ct = default)
    {
        var payload = JsonSerializer.JsonSerializeMessage(e);

        await _dbContext.OutboxEvents.AddAsync(new OutboxEvent
        {
            IdempotencyKey = idempotencyKey,
            EventType = eventType,
            Payload = payload,
            OccuredAt = DateTime.UtcNow,
            IsSend = false
        }, ct);
    }

    public Task<List<OutboxEvent>> RetrieveUnsendEventsAsync(EventType eventType, CancellationToken ct = default)
        => _dbContext.OutboxEvents.AsNoTracking().Where(x => x.IsSend == false).ToListAsync(cancellationToken: ct);

    public async Task MarkAsSendAsync(Guid idempotencyKey, CancellationToken ct = default)
    {
        var outboxEvent = await _dbContext.OutboxEvents.SingleAsync(x => x.IdempotencyKey == idempotencyKey, ct);
        outboxEvent.IsSend = true;

        await _dbContext.SaveChangesAsync(ct);
    }
}