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
        private readonly IAsyncConnectionFactory _factory;
        private readonly IConnection connection;

        public RabbitMessageQueueService(IAsyncConnectionFactory factory)
        {
            _factory = factory;
            connection = _factory.CreateConnection();
        }
        public void SendMessage<T>(string queue, T message)
        {
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: queue,
                durable: false, exclusive: false,
                autoDelete: false, arguments: null);

            channel.BasicPublish(
                exchange: string.Empty,
                routingKey: queue,
                basicProperties: null,
                body: message.ToUTF8JsonByteArray());
        }
    }
}
