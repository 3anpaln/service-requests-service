using service_requests_service.Models;
using service_requests_service.Persistent.Entities;

namespace service_requests_service.Contracts;

public interface IServiceRequestService
{
    public Task<ServiceRequest> CreateAsync(CreateServiceRequestRequest request, CancellationToken ct = default);
    public Task UpdateStatusAsync(Guid id, ServiceRequestStatus status, CancellationToken ct = default);
    public Task<ServiceRequest?> GetAsync(Guid id, CancellationToken ct = default);
    public Task<IEnumerable<ServiceRequest>> GetAllAsync(CancellationToken ct = default);
}