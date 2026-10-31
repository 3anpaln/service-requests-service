using service_requests_service.Contracts;

namespace service_requests_service.Services;

public class InboxService : IInboxService
{
    public Task<bool> IsInboxEventProcessed(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task StoreInboxAsync(Guid id, string consumerName, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}