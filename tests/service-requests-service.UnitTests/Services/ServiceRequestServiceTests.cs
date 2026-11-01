using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using service_requests_service.Bus.Messages;
using service_requests_service.Contracts;
using service_requests_service.Exceptions;
using service_requests_service.Models;
using service_requests_service.Persistent.Data;
using service_requests_service.Persistent.Entities;
using service_requests_service.Services;
using Xunit;

namespace srt_test.UnitTests.Services;

public sealed class ServiceRequestServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly Mock<IOutboxService> _outbox = new(MockBehavior.Strict);
    private readonly Mock<ILogger<ServiceRequestService>> _logger = new();
    private readonly SaveFailureInterceptor _saveFailure = new();
    private readonly AppDbContext _db;
    private readonly ServiceRequestService _sut;

    public ServiceRequestServiceTests()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(_saveFailure)
            .Options);
        _sut = new ServiceRequestService(_db, _outbox.Object, _logger.Object);

        // Stage a real row so assertions also detect missing SaveChanges/Commit
        // and partial writes on rollback, while keeping the outbox dependency mocked.
        _outbox.Setup(x => x.StoreEventWithoutSaveChangesAsync(
                It.IsAny<Guid>(), It.IsAny<EventType>(),
                It.IsAny<UpsertServiceRequestMessage>(), It.IsAny<CancellationToken>()))
            .Returns((Guid key, EventType type, UpsertServiceRequestMessage message, CancellationToken _) =>
            {
                _db.OutboxEvents.Add(new OutboxEvent
                {
                    IdempotencyKey = key,
                    EventType = type,
                    Payload = JsonSerializer.JsonSerializeMessage(message),
                    OccuredAt = DateTime.UtcNow,
                    IsSend = false
                });
                return Task.CompletedTask;
            });
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        await _db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_WithExplicitPriority_PersistsRequestAndMatchingOutboxEvent()
    {
        using var cancellation = new CancellationTokenSource();
        var request = new CreateServiceRequestRequest
        {
            Title = "Restore service",
            Description = "Customers cannot submit requests.",
            Priority = ServiceRequestPriority.High
        };

        var result = await _sut.CreateAsync(request, cancellation.Token);

        var stored = await _db.ServiceRequests.AsNoTracking().SingleAsync();
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(result.Id, stored.Id);
        Assert.Equal(request.Title, stored.Title);
        Assert.Equal(request.Description, stored.Description);
        Assert.Equal(ServiceRequestPriority.High, stored.Priority);
        Assert.Equal(ServiceRequestStatus.New, stored.Status);
        Assert.NotEqual(default, stored.CreatedAt);
        Assert.Null(stored.UpdatedAt);
        await AssertOutboxEventAsync(stored, null, cancellation.Token);
    }

    [Fact]
    public async Task CreateAsync_WithoutPriority_UsesNormalPriority()
    {
        var result = await _sut.CreateAsync(new CreateServiceRequestRequest { Title = "New request" });

        var stored = await _db.ServiceRequests.AsNoTracking().SingleAsync();
        Assert.Equal(result.Id, stored.Id);
        Assert.Equal(ServiceRequestPriority.Normal, stored.Priority);
        Assert.Null(stored.Description);
        await AssertOutboxEventAsync(stored, null);
    }

    [Fact]
    public Task UpdateStatusAsync_NewToInProgress_SavesTransitionAndEvent() =>
        AssertAllowedTransitionAsync(ServiceRequestStatus.New, ServiceRequestStatus.InProgress);

    [Fact]
    public Task UpdateStatusAsync_NewToCancelled_SavesTransitionAndEvent() =>
        AssertAllowedTransitionAsync(ServiceRequestStatus.New, ServiceRequestStatus.Cancelled);

    [Fact]
    public Task UpdateStatusAsync_InProgressToDone_SavesTransitionAndEvent() =>
        AssertAllowedTransitionAsync(ServiceRequestStatus.InProgress, ServiceRequestStatus.Done);

    [Fact]
    public Task UpdateStatusAsync_InProgressToCancelled_SavesTransitionAndEvent() =>
        AssertAllowedTransitionAsync(ServiceRequestStatus.InProgress, ServiceRequestStatus.Cancelled);

    [Fact]
    public Task UpdateStatusAsync_CancelledToNew_SavesTransitionAndEvent() =>
        AssertAllowedTransitionAsync(ServiceRequestStatus.Cancelled, ServiceRequestStatus.New);

    [Fact]
    public Task UpdateStatusAsync_NewToNew_RejectsTransition() =>
        AssertRejectedTransitionAsync(ServiceRequestStatus.New, ServiceRequestStatus.New);

    [Fact]
    public Task UpdateStatusAsync_NewToDone_RejectsTransition() =>
        AssertRejectedTransitionAsync(ServiceRequestStatus.New, ServiceRequestStatus.Done);

    [Fact]
    public Task UpdateStatusAsync_InProgressToNew_RejectsTransition() =>
        AssertRejectedTransitionAsync(ServiceRequestStatus.InProgress, ServiceRequestStatus.New);

    [Fact]
    public Task UpdateStatusAsync_InProgressToInProgress_RejectsTransition() =>
        AssertRejectedTransitionAsync(ServiceRequestStatus.InProgress, ServiceRequestStatus.InProgress);

    [Fact]
    public Task UpdateStatusAsync_DoneToNew_RejectsTransition() =>
        AssertRejectedTransitionAsync(ServiceRequestStatus.Done, ServiceRequestStatus.New);

    [Fact]
    public Task UpdateStatusAsync_DoneToInProgress_RejectsTransition() =>
        AssertRejectedTransitionAsync(ServiceRequestStatus.Done, ServiceRequestStatus.InProgress);

    [Fact]
    public Task UpdateStatusAsync_DoneToDone_RejectsTransition() =>
        AssertRejectedTransitionAsync(ServiceRequestStatus.Done, ServiceRequestStatus.Done);

    [Fact]
    public Task UpdateStatusAsync_DoneToCancelled_RejectsTransition() =>
        AssertRejectedTransitionAsync(ServiceRequestStatus.Done, ServiceRequestStatus.Cancelled);

    [Fact]
    public Task UpdateStatusAsync_CancelledToInProgress_RejectsTransition() =>
        AssertRejectedTransitionAsync(ServiceRequestStatus.Cancelled, ServiceRequestStatus.InProgress);

    [Fact]
    public Task UpdateStatusAsync_CancelledToDone_RejectsTransition() =>
        AssertRejectedTransitionAsync(ServiceRequestStatus.Cancelled, ServiceRequestStatus.Done);

    [Fact]
    public Task UpdateStatusAsync_CancelledToCancelled_RejectsTransition() =>
        AssertRejectedTransitionAsync(ServiceRequestStatus.Cancelled, ServiceRequestStatus.Cancelled);

    [Fact]
    public Task UpdateStatusAsync_WithUndefinedStatus_RejectsTransition() =>
        AssertRejectedTransitionAsync(ServiceRequestStatus.New, (ServiceRequestStatus)999);

    [Fact]
    public async Task UpdateStatusAsync_WhenRequestDoesNotExist_ThrowsWithoutStoringEvent()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateStatusAsync(Guid.NewGuid(), ServiceRequestStatus.InProgress));

        Assert.Empty(await _db.OutboxEvents.AsNoTracking().ToListAsync());
        _outbox.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenOutboxFails_DoesNotPersistRequestAndRethrows()
    {
        var failure = FailOutbox();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.CreateAsync(new CreateServiceRequestRequest { Title = "New request" }));

        Assert.Same(failure, actual);
        Assert.Empty(await _db.ServiceRequests.AsNoTracking().ToListAsync());
        Assert.Empty(await _db.OutboxEvents.AsNoTracking().ToListAsync());
        VerifyOneOutboxCall();
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenOutboxFails_DoesNotPersistStatusAndRethrows()
    {
        var request = await SeedRequestAsync(ServiceRequestStatus.InProgress);
        var failure = FailOutbox();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateStatusAsync(request.Id, ServiceRequestStatus.Done));

        Assert.Same(failure, actual);
        var stored = await _db.ServiceRequests.AsNoTracking().SingleAsync();
        Assert.Equal(ServiceRequestStatus.InProgress, stored.Status);
        Assert.Empty(await _db.OutboxEvents.AsNoTracking().ToListAsync());
        VerifyOneOutboxCall();
    }

    [Fact]
    public async Task CreateAsync_WhenSaveFailsAfterWriting_RollsBackRequestAndOutboxEvent()
    {
        var failure = _saveFailure.Failure = new InvalidOperationException("Simulated save failure.");

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.CreateAsync(new CreateServiceRequestRequest { Title = "New request" }));

        Assert.Same(failure, actual);
        Assert.Empty(await _db.ServiceRequests.AsNoTracking().ToListAsync());
        Assert.Empty(await _db.OutboxEvents.AsNoTracking().ToListAsync());
        VerifyOneOutboxCall();
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenSaveFailsAfterWriting_RollsBackStatusAndOutboxEvent()
    {
        var request = await SeedRequestAsync(ServiceRequestStatus.InProgress);
        var failure = _saveFailure.Failure = new InvalidOperationException("Simulated save failure.");

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateStatusAsync(request.Id, ServiceRequestStatus.Done));

        Assert.Same(failure, actual);
        var stored = await _db.ServiceRequests.AsNoTracking().SingleAsync();
        Assert.Equal(ServiceRequestStatus.InProgress, stored.Status);
        Assert.Empty(await _db.OutboxEvents.AsNoTracking().ToListAsync());
        VerifyOneOutboxCall();
    }

    [Fact]
    public async Task CreateAsync_WhenAlreadyCancelled_DoesNotPersistRequestOrEvent()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _sut.CreateAsync(new CreateServiceRequestRequest { Title = "New request" }, cancellation.Token));

        Assert.Empty(await _db.ServiceRequests.AsNoTracking().ToListAsync());
        Assert.Empty(await _db.OutboxEvents.AsNoTracking().ToListAsync());
        _outbox.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenAlreadyCancelled_DoesNotPersistStatusOrEvent()
    {
        var request = await SeedRequestAsync(ServiceRequestStatus.New);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _sut.UpdateStatusAsync(request.Id, ServiceRequestStatus.InProgress, cancellation.Token));

        var stored = await _db.ServiceRequests.AsNoTracking().SingleAsync();
        Assert.Equal(ServiceRequestStatus.New, stored.Status);
        Assert.Empty(await _db.OutboxEvents.AsNoTracking().ToListAsync());
        _outbox.VerifyNoOtherCalls();
    }

    private async Task AssertAllowedTransitionAsync(ServiceRequestStatus oldStatus, ServiceRequestStatus newStatus)
    {
        var request = await SeedRequestAsync(oldStatus);
        using var cancellation = new CancellationTokenSource();

        await _sut.UpdateStatusAsync(request.Id, newStatus, cancellation.Token);

        var stored = await _db.ServiceRequests.AsNoTracking().SingleAsync();
        Assert.Equal(request.Id, stored.Id);
        Assert.Equal(newStatus, stored.Status);
        Assert.Equal(request.Title, stored.Title);
        Assert.Equal(request.Description, stored.Description);
        Assert.Equal(request.Priority, stored.Priority);
        await AssertOutboxEventAsync(stored, oldStatus, cancellation.Token);
    }

    private async Task AssertRejectedTransitionAsync(ServiceRequestStatus oldStatus, ServiceRequestStatus newStatus)
    {
        var request = await SeedRequestAsync(oldStatus);

        var exception = await Assert.ThrowsAsync<UnauthorizedTransitionException>(() =>
            _sut.UpdateStatusAsync(request.Id, newStatus));

        Assert.Equal($"Cannot change status from {oldStatus} to {newStatus}.", exception.Message);
        Assert.Equal(oldStatus, request.Status);
        var stored = await _db.ServiceRequests.AsNoTracking().SingleAsync();
        Assert.Equal(oldStatus, stored.Status);
        Assert.Empty(await _db.OutboxEvents.AsNoTracking().ToListAsync());
        _outbox.VerifyNoOtherCalls();
        _logger.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((value, _) => value.ToString()!.Contains(request.Id.ToString())),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    private async Task<ServiceRequest> SeedRequestAsync(ServiceRequestStatus status)
    {
        var request = new ServiceRequest("Existing request", "Keep these details", ServiceRequestPriority.High)
        {
            Status = status
        };
        _db.ServiceRequests.Add(request);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        return request;
    }

    private async Task AssertOutboxEventAsync(ServiceRequest request, ServiceRequestStatus? oldStatus,
        CancellationToken ct = default)
    {
        var storedEvent = await _db.OutboxEvents.AsNoTracking().SingleAsync();
        Assert.NotEqual(Guid.Empty, storedEvent.IdempotencyKey);
        Assert.Equal(EventType.UpsertServiceRequestEvet, storedEvent.EventType);
        Assert.False(storedEvent.IsSend);
        _outbox.Verify(x => x.StoreEventWithoutSaveChangesAsync(
            storedEvent.IdempotencyKey,
            EventType.UpsertServiceRequestEvet,
            It.Is<UpsertServiceRequestMessage>(message =>
                message.IdempotencyKey == storedEvent.IdempotencyKey &&
                message.ServiceRequestId == request.Id &&
                message.Priority == request.Priority &&
                message.OldStatus == oldStatus &&
                message.NewStatus == request.Status), ct), Times.Once);
        _outbox.VerifyNoOtherCalls();
    }

    private InvalidOperationException FailOutbox()
    {
        var failure = new InvalidOperationException("Simulated outbox failure.");
        _outbox.Setup(x => x.StoreEventWithoutSaveChangesAsync(
                It.IsAny<Guid>(), It.IsAny<EventType>(),
                It.IsAny<UpsertServiceRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
        return failure;
    }

    private void VerifyOneOutboxCall()
    {
        _outbox.Verify(x => x.StoreEventWithoutSaveChangesAsync(
            It.IsAny<Guid>(), EventType.UpsertServiceRequestEvet,
            It.IsAny<UpsertServiceRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _outbox.VerifyNoOtherCalls();
    }

    private sealed class SaveFailureInterceptor : SaveChangesInterceptor
    {
        public Exception? Failure { get; set; }

        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            if (Failure is not null)
                throw Failure;

            return ValueTask.FromResult(result);
        }
    }
}
