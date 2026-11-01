using Microsoft.EntityFrameworkCore;
using service_requests_service.Contracts;
using service_requests_service.Persistent.Data;
using service_requests_service.Persistent.Entities;

namespace service_requests_service.Services;

//For production possibly need to create repositories
public sealed class InboxService : IInboxService
{
    private readonly AppDbContext _dbContext;

    public InboxService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsInboxEventProcessed(Guid id, CancellationToken ct = default)
    {
        var storedEvent = await _dbContext.InboxEvents.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == id, cancellationToken: ct);
        return storedEvent != null;
    }

    public async Task StoreInboxAsync(Guid id, string consumerName, CancellationToken ct = default)
    {
        //For production and testing issues need to move getting DateTime to separate service
        await _dbContext.InboxEvents.AddAsync(
            new InboxEvent { IdempotencyKey = id, ConsumerName = consumerName, ProcessedAt = DateTime.UtcNow }, ct);
        await _dbContext.SaveChangesAsync(ct);
    }
}