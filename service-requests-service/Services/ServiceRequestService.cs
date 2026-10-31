using Microsoft.EntityFrameworkCore;
using service_requests_service.Bus.Messages;
using service_requests_service.Contracts;
using service_requests_service.Exceptions;
using service_requests_service.Models;
using service_requests_service.Persistent.Data;
using service_requests_service.Persistent.Entities;

namespace service_requests_service.Services;

public class ServiceRequestService : IServiceRequestService
{
    private readonly AppDbContext _dbContext;
    private readonly IOutboxService _outboxService;
    private readonly ILogger<ServiceRequestService> _logger;

    public ServiceRequestService(AppDbContext dbContext, IOutboxService outboxService,
        ILogger<ServiceRequestService> logger)
    {
        _dbContext = dbContext;
        _outboxService = outboxService;
        _logger = logger;
    }

    //For production guid normally creates in separate layer
    public async Task<ServiceRequest> CreateAsync(CreateServiceRequestRequest request, CancellationToken ct = default)
    {
        var sr = new ServiceRequest(request.Title, request.Description, request.Priority);

        var message = new UpsertServiceRequestMessage(sr.Id, null, sr.Status, sr.Priority);

        //Can be overriden by async version 
        await PrepareAndSaveEvent(message, () => _dbContext.ServiceRequests.Add(sr), ct);

        return sr;
    }

    public async Task UpdateStatusAsync(Guid id, ServiceRequestStatus status,
        CancellationToken ct = default)
    {
        var serviceRequest = await _dbContext.ServiceRequests.Where(x => x.Id == id)
            .SingleAsync(cancellationToken: ct);

        //For production better way to use StateMachine with all transitions
        if (!IsTransitionValid(serviceRequest.Status, status))
        {
            _logger.LogWarning(
                $"Unauthorized status change detected for {serviceRequest.Id}: transition from  {serviceRequest.Status} to {status}.");

            throw new UnauthorizedTransitionException(serviceRequest.Status, status);
        }

        var message =
            new UpsertServiceRequestMessage(serviceRequest.Id, serviceRequest.Status, status, serviceRequest.Priority);
        serviceRequest.Status = status;

        await PrepareAndSaveEvent(message, () => _dbContext.ServiceRequests.Update(serviceRequest), ct);
    }

    public Task<ServiceRequest?> GetAsync(Guid id, CancellationToken ct = default)
        => _dbContext.ServiceRequests.SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task<IEnumerable<ServiceRequest>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(_dbContext.ServiceRequests.AsNoTracking().AsEnumerable());

    private async Task PrepareAndSaveEvent(UpsertServiceRequestMessage message, Action dbManipulationAction,
        CancellationToken ct = default)
    {
        //Transaction to ensure that two changes occur simultaneously
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            await _outboxService.StoreEventWithoutSaveChangesAsync(message.IdempotencyKey,
                EventType.UpsertServiceRequestEvet,
                message, ct);

            dbManipulationAction.Invoke();

            await _dbContext.SaveChangesAsync(ct);

            //All ok - commit
            await transaction.CommitAsync(ct);
        }
        catch (Exception)
        {
            //Fall - no changes were saved
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static bool IsTransitionValid(ServiceRequestStatus oldStatus, ServiceRequestStatus newStatus)
        =>
            (oldStatus, newStatus) switch
            {
                (ServiceRequestStatus.New, ServiceRequestStatus.InProgress or ServiceRequestStatus.Cancelled) => true,
                (ServiceRequestStatus.InProgress, ServiceRequestStatus.Done or ServiceRequestStatus.Cancelled) => true,
                (ServiceRequestStatus.Cancelled, ServiceRequestStatus.New) => true,
                _ => false
            };
}