using ExchangeRateManager.Common.Interfaces.Base;
using ExchangeRateManager.Dtos;

namespace ExchangeRateManager.Services.Interfaces;

public interface IExchangeRateService : IService
{
    Task<ExchangeRateResponseDto> GetForexLiveRate(ExchangeRateRequestDto exchangeRate);
    Task<ExchangeRateResponseDto> GetForexRate(ExchangeRateRequestDto exchangeRate);
    Task<ExchangeRateResponseDto> GetForexStoredRate(ExchangeRateRequestDto exchangeRate);
    Task UpdateForexRate(ExchangeRateResponseDto rateToUpdate, DateTime? lastKnownRefreshedDate = null);
}
