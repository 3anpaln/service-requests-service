using Microsoft.EntityFrameworkCore;
using service_requests_service.Bus.Common;
using service_requests_service.Bus.Messages;
using service_requests_service.Contracts;
using service_requests_service.Models;
using service_requests_service.Persistent.Data;
using service_requests_service.Persistent.Entities;
using service_requests_service.Services;

namespace service_requests_service.Bus.Consumers;

public sealed class UpsertServiceRequestEventConsumer : IRabbitMqConsumer<UpsertServiceRequestMessage>
{
    private readonly ILogger<UpsertServiceRequestEventConsumer> _logger;
    private readonly AppDbContext _dbContext;

    public UpsertServiceRequestEventConsumer(ILogger<UpsertServiceRequestEventConsumer> logger, AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public UpsertServiceRequestMessage DeserializeMessage(byte[] message) =>
        JsonSerializer.JsonDeserializeMessage<UpsertServiceRequestMessage>(message);

    public async Task<bool> ConsumeAsync(UpsertServiceRequestMessage message)
    {
        try
        {
            var isAlreadyProceed = await _dbContext.InboxEvents.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdempotencyKey == message.IdempotencyKey) != null;
            if (isAlreadyProceed)
            {
                _logger.LogInformation(
                    $"The service request event: {message.ServiceRequestId} and idempotency key: {message.IdempotencyKey} is already processed.");
                return true;
            }

            if (message.Priority == ServiceRequestPriority.High || message.NewStatus == ServiceRequestStatus.Done)
                _logger.LogInformation(
                    $"Service request with id: {message.ServiceRequestId}, priority: {message.Priority}, newstatus: {message.NewStatus} has been processed'.");

            if (message.NewStatus == ServiceRequestStatus.Done)
            {
                var request = await _dbContext.ServiceRequests.SingleAsync(x => x.Id == message.ServiceRequestId);
                await _dbContext.DoneServiceRequests.AddAsync(new DoneServiceRequest
                {
                    Id = request.Id,
                    Title = request.Title,
                    Description = request.Description,
                    Priority = request.Priority,
                    //Must already save as Done
                    Status = request.Status,
                    CreatedAt = request.UpdatedAt ?? DateTime.UtcNow
                });
            }

            return true;
        }
        catch (Exception)
        {
            //For production need more information about why and when, and possible retry mechanism
            return false;
        }
    }

    //All below must be const(only one instance for application!)
    public IExchangeInfo ExchangeInfo => new UpsertServiceRequestExchangeInfo();
    public IQueueInfo QueueInfo => new UpsertServiceRequestQueueInfo();
}