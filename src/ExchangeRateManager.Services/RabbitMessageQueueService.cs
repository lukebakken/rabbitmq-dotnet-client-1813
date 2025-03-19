using ExchangeRateManager.Common.Extensions;
using ExchangeRateManager.Common.Interfaces.ServiceLifetime;
using ExchangeRateManager.Services.Interfaces;
using RabbitMQ.Client;

namespace ExchangeRateManager.Services
{
    /// <summary>
    /// RabbitMQ message queue service
    /// </summary>
    public class RabbitMessageQueueService : IMessageQueueService, IScoped
    {
        private readonly IConnectionFactory _factory;
        private readonly IConnection connection;

        public RabbitMessageQueueService(IConnectionFactory factory)
        {
            _factory = factory;
            // Note: there should be a StartAsync or similar method for these service classes.
            connection = _factory.CreateConnectionAsync().GetAwaiter().GetResult();
        }

        public async Task SendMessageAsync<T>(string queue, T message)
        {
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: queue,
                durable: false, exclusive: false,
                autoDelete: false, arguments: null);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                body: message.ToUTF8JsonByteArray());
        }
    }
}
