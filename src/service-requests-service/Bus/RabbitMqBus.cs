using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using service_requests_service.Contracts;

namespace service_requests_service.Bus;

public sealed class RabbitMqBus : IRabbitMqBus, IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqBus> _logger;
    private IConnection? _connection;

    //For many connections possibly need complicated logic
    private readonly Dictionary<string, IChannel> _channels;

    public RabbitMqBus(IConfiguration configuration, ILogger<RabbitMqBus> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _channels = new Dictionary<string, IChannel>();
    }

    private IConnection Connection =>
        _connection ?? throw new InvalidOperationException(
            "RabbitMQ is not initialized.");

    public async Task StartAsync(Func<IRabbitMqBus, Task> madeSubscriptions, CancellationToken ct = default)
    {
        _connection = await CreateConnectionFactory().CreateConnectionAsync(ct);
        await madeSubscriptions(this);
    }

    public async Task SubscribeAsync<TConsumer, TMessage>(TConsumer consumerInstance, CancellationToken ct = default)
        where TConsumer : IRabbitMqConsumer<TMessage>
        where TMessage : IRabbitMqMessage
    {
        var channel = await GetOrAddChannelAsync<TMessage>();

        await channel.ExchangeDeclareAsync(
            consumerInstance.ExchangeInfo.ExchangeName,
            consumerInstance.ExchangeInfo.ExchangeType,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        await channel.QueueDeclareAsync(
            consumerInstance.QueueInfo.QueueName,
            consumerInstance.QueueInfo.IsDurable,
            consumerInstance.QueueInfo.IsExclusive,
            consumerInstance.QueueInfo.IsAutoDelete, cancellationToken: ct);
        await channel.QueueBindAsync(consumerInstance.QueueInfo.QueueName, consumerInstance.ExchangeInfo.ExchangeName,
            consumerInstance.ExchangeInfo.RoutingKey, cancellationToken: ct);

        var consumer = InitializeConsumer<TConsumer, TMessage>(channel, consumerInstance, ct);

        await channel.BasicConsumeAsync(consumerInstance.QueueInfo.QueueName, consumerInstance.QueueInfo.IsAutoAck, consumer, cancellationToken: ct);
    }

    public async Task PublishAsync<TMessage>(IRabbitMqEvent<TMessage> message, CancellationToken ct = default)
        where TMessage : IRabbitMqMessage
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        try
        {
            var channel = await GetOrAddChannelAsync<TMessage>();
            await channel.ExchangeDeclareAsync(message.ExchangeInfo.ExchangeName, message.ExchangeInfo.ExchangeType,
                true, false, cancellationToken: ct);

            var serializedMessage = message.Serialize(message.Body);

            await channel.BasicPublishAsync(message.ExchangeInfo.ExchangeName, message.ExchangeInfo.RoutingKey,
                body: serializedMessage, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            //For production possibly need more complex errors logic
            _logger.LogError($"Something went wrong while publishing message: '{message.GetType().Name}'.",
                ex);
            throw;
        }
    }

    private AsyncEventingBasicConsumer InitializeConsumer<TConsumer, TMessage>(IChannel channel,
        TConsumer consumerInstance, CancellationToken ct = default)
        where TConsumer : IRabbitMqConsumer<TMessage>
        where TMessage : IRabbitMqMessage
    {
        var consumer = new AsyncEventingBasicConsumer(channel);
        
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var message = consumerInstance.DeserializeMessage(ea.Body.ToArray());
                var result = await consumerInstance.ConsumeAsync(message);

                if (result)
                    await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                //For production need logic for few time retrying and put to DLQ
                else
                    await channel.BasicNackAsync(ea.DeliveryTag, false, false, ct);

                await Task.Yield();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong while consuming... {typeof(TConsumer).Name}'.",
                    ex);
                throw;
            }
        };
        
        return consumer;
    }

    private ConnectionFactory CreateConnectionFactory()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"]
                       ?? throw new InvalidOperationException("RabbitMQ Host is missing."),

            Port = _configuration.GetValue(
                "RabbitMQ:Port", 5672),

            UserName = _configuration["RabbitMQ:Username"]
                       ?? throw new InvalidOperationException(
                           "RabbitMQ username is missing."),

            Password = _configuration["RabbitMQ:Password"]
                       ?? throw new InvalidOperationException(
                           "RabbitMQ password is missing."),

            VirtualHost = _configuration["RabbitMQ:VirtualHost"]
                          ?? "/",

            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };

        return factory;
    }

    private async Task<IChannel> GetOrAddChannelAsync<T>()
    {
        var channelName = typeof(T).Name;
        if (_channels.TryGetValue(channelName, out var existsChannel))
            return existsChannel;

        var channel = await Connection.CreateChannelAsync();
        _channels[channelName] = channel;

        return channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}