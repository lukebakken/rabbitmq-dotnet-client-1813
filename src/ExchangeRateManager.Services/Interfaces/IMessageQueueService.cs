using ExchangeRateManager.Common.Interfaces.Base;

namespace ExchangeRateManager.Services.Interfaces;

/// <summary>
/// Message Queue service interface.
/// </summary>
public interface IMessageQueueService : IService
{
    void SendMessage<T>(string queue, T message);
}