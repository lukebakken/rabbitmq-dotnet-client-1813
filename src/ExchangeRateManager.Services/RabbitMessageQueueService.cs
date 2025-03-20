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

        public RabbitMessageQueueService(IConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task SendMessage<T>(string queue, T message, CancellationToken cancellationToken = default)
        {
            using var connection = await _factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: queue,
                durable: false, exclusive: false,
                autoDelete: false, arguments: null);

            await channel.BasicPublishAsync(
                string.Empty, queue,
                message.ToUTF8JsonByteArray(),
                cancellationToken);
        }
    }
}
