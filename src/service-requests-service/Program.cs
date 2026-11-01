using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using service_requests_service.BackgroundServices;
using service_requests_service.Bus;
using service_requests_service.Bus.Consumers;
using service_requests_service.Bus.Messages;
using service_requests_service.Contracts;
using service_requests_service.Exceptions;
using service_requests_service.Models;
using service_requests_service.Persistent.Data;
using service_requests_service.Services;
using service_requests_service.Validators;

var builder = WebApplication.CreateBuilder(args);

RegisterPostgres(builder);
RegisterServices(builder);
RegisterRabbitMq(builder);
RegisterValidators(builder);
RegisterExceptionHandler(builder);

var app = builder.Build();

app.UseExceptionHandler();

//Apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();
}

await StartRabbitMqBusAndDoSubscribeAsync(app);

//For production must check deeper
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy"
}));

//200, 404
app.MapGet("/api/service-requests/{id}", async ([FromRoute] Guid id, IServiceRequestService service, CancellationToken ct) =>
{
    var serviceRequest = await service.GetAsync(id, ct);

    return serviceRequest != null ? Results.Ok(serviceRequest) : Results.NotFound();
});

//200
app.MapGet("/api/service-requests", async (IServiceRequestService service, CancellationToken ct)
    => await service.GetAllAsync(ct));

//204, 400, 409
app.MapPatch("/api/service-requests/{id}/status",
    async ([FromRoute] Guid id, [FromBody] ServiceRequestStatus status, IServiceRequestService service, CancellationToken ct) =>
    {
        await service.UpdateStatusAsync(id, status, ct);
        
        return Results.NoContent();
    });

//201, 400
app.MapPost("/api/service-requests", async ([FromBody] CreateServiceRequestRequest request,
    IValidator<CreateServiceRequestRequest> validator, IServiceRequestService service, CancellationToken ct) =>
{
    var result = await validator.ValidateAsync(request, ct);

    if (!result.IsValid)
    {
        return Results.ValidationProblem(
            result.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(e => e.ErrorMessage).ToArray()));
    }

    var sr = await service.CreateAsync(request, ct);

    return Results.Created($"/api/service-requests/{sr.Id}", sr);
});

app.Run();
return;

#region Registrations

// For production separate registration to specific layer
void RegisterPostgres(WebApplicationBuilder b)
{
    var postgresConnection =
        b.Configuration.GetConnectionString("PostgresSQL")!;
    b.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(postgresConnection));
}

// For production separate registration to specific layer
void RegisterServices(WebApplicationBuilder b)
{
    b.Services.AddLogging();
    
    b.Services.AddScoped<IInboxService, InboxService>();
    b.Services.AddScoped<IOutboxService, OutboxService>();
    b.Services.AddScoped<IServiceRequestService, ServiceRequestService>();
    
    b.Services.AddHostedService<OutboxBackgroundService>();
}

// For production separate registration to specific layer
void RegisterRabbitMq(WebApplicationBuilder b)
{
    b.Services.AddSingleton<IRabbitMqBus, RabbitMqBus>();
    b.Services.AddScoped<IRabbitMqConsumer<UpsertServiceRequestMessage>, UpsertServiceRequestEventConsumer>();
}

// For production separate registration to specific layer/move to specific place on this
void RegisterValidators(WebApplicationBuilder b)
{
    b.Services.AddValidatorsFromAssemblyContaining<CreateServiceRequestValidator>();
}

// For production separate registration to specific layer/move to specific place on this
void RegisterExceptionHandler(WebApplicationBuilder webApplicationBuilder)
{
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
}

#endregion

#region App Actions

// For production separate start to specific layer
async Task StartRabbitMqBusAndDoSubscribeAsync(WebApplication wa)
{
    var bus = wa.Services.GetRequiredService<IRabbitMqBus>();
    await using var scope = wa.Services.CreateAsyncScope();
    var upsertConsumer = scope.ServiceProvider
        .GetRequiredService<IRabbitMqConsumer<UpsertServiceRequestMessage>>();

    await bus.StartAsync(async initializedBus =>
    {
        await initializedBus.SubscribeAsync<IRabbitMqConsumer<UpsertServiceRequestMessage>, UpsertServiceRequestMessage>(
            upsertConsumer);
    });
}
#endregion