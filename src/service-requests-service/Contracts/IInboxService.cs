namespace service_requests_service.Contracts;

public interface IInboxService
{
    public Task<bool> IsInboxEventProcessed(Guid id, CancellationToken ct = default);
    public Task StoreInboxAsync(Guid id, string consumerName, CancellationToken ct = default);
}