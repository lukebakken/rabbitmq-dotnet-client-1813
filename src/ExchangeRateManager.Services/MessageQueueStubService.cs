using ExchangeRateManager.Common.Interfaces.ServiceLifetime;
using ExchangeRateManager.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExchangeRateManager.Services
{
    /// <summary>
    /// Message queue stub for tests and development purposes.
    /// </summary>
    public class MessageQueueStubService(
        ILogger<MessageQueueStubService> logger) : IMessageQueueService, IScoped
    {
        private readonly ILogger<MessageQueueStubService> _logger = logger;

        public Task SendMessageAsync<T>(string queue, T message)
        {
            _logger.LogInformation(
                "[{method}] Got a message for '{queue}' queue. Type: {type}; message: {message}",
                nameof(SendMessageAsync), typeof(T).FullName, queue, message);
            return Task.CompletedTask;
        }
    }
}
