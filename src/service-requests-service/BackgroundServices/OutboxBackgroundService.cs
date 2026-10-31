using service_requests_service.Bus.Events;
using service_requests_service.Bus.Messages;
using service_requests_service.Contracts;
using service_requests_service.Models;
using service_requests_service.Services;

namespace service_requests_service.BackgroundServices;

public sealed class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxBackgroundService> _logger;

    public OutboxBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        //For production use value from configuration file or if it will change, it need more complex logic
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        do
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processing failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessOutboxAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var bus = scope.ServiceProvider.GetRequiredService<IRabbitMqBus>();
        var events = await outboxService.RetrieveUnsendEventsAsync(EventType.UpsertServiceRequestEvet, ct);

        foreach (var outboxEvent in events)
        {
            var rabbitMqEvent =
                new UpsertServiceRequestEvent(
                    JsonSerializer.JsonDeserializeMessage<UpsertServiceRequestMessage>(outboxEvent.Payload));

            await bus.PublishAsync(rabbitMqEvent, ct);
            await outboxService.MarkAsSendAsync(rabbitMqEvent.Body.IdempotencyKey, ct);
        }
    }
}