using AutoMapper;
using ExchangeRateManager.ApiClients.Interfaces;
using ExchangeRateManager.ApiClients.Responses;
using ExchangeRateManager.Common.Configuration;
using ExchangeRateManager.Common.Constants;
using ExchangeRateManager.Common.Exceptions;
using ExchangeRateManager.Common.Interfaces.ServiceLifetime;
using ExchangeRateManager.Dtos;
using ExchangeRateManager.Repositories.Entities;
using ExchangeRateManager.Repositories.Interfaces;
using ExchangeRateManager.Services.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExchangeRateManager.Services;

/// <summary>
/// Gets the latest Forex (Foreign Exchange) Rate between two currencies.
/// </summary>
public class ExchangeRateService(IForexClient forexClient,
    IForexRateRepository forexRateRepository, IMessageQueueService messageQueueService,
    IBackgroundJobClient backgroundJobClient, IOptionsSnapshot<Settings> settings,
    IMapper mapper, ILogger<ExchangeRateService> logger) : IExchangeRateService, IScoped
{
    private readonly IMapper _mapper = mapper;
    private readonly IForexClient _forexClient = forexClient;
    private readonly IForexRateRepository _forexRateRepository = forexRateRepository;
    private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;
    private readonly IMessageQueueService _messageQueueService = messageQueueService;
    private readonly ILogger<ExchangeRateService> _logger = logger;

    private Settings Settings => settings.Value;

    /// <summary>
    /// Gets the latest Forex (Foreign Exchange) Rate between two currencies.
    /// </summary>
    public async Task<ExchangeRateResponseDto> GetForexRate(ExchangeRateRequestDto exchangeRate)
    {
        var response = await (Settings.ExchangeRate.PreferLiveData
          ? GetForexLiveRate(exchangeRate)       //Option 1: Prefer realtime, falls back into cached when limit is reached.
          : GetForexStoredRate(exchangeRate));   //Option 2: Prefer cached, expire periodically.

        return response!;
    }

    /// <summary>
    /// Updates the stored/cached rate and sends a message if the rate is newer.
    /// </summary>
    public async Task UpdateForexRate(ExchangeRateResponseDto rateToUpdate, DateTime? lastKnownRefreshedDate = default)
    {
        await _forexRateRepository.Upsert(_mapper.Map<ForexRateEntity>(rateToUpdate));
        var isLatest = lastKnownRefreshedDate == default || lastKnownRefreshedDate < rateToUpdate.LastRefreshed;
        if (isLatest)
        {
            _messageQueueService.SendMessage(MessageQueues.NewForexRate, rateToUpdate);
        }
    }

    /// <summary>
    /// Gets the latest Forex Rate from the source then updates the stored/cached for reuse later.
    /// </summary>
    public async Task<ExchangeRateResponseDto> GetForexLiveRate(ExchangeRateRequestDto exchangeRate)
    {
        var getCurrencyExchangeRatesTask = Task.FromResult(default(GetCurrencyExchangeRatesResponse));
        var getFromEntityTask = Task.FromResult(default(ForexRateEntity));
        ExchangeRateResponseDto? result;

        try
        {
            getCurrencyExchangeRatesTask = _forexClient
                .GetCurrencyExchangeRates(exchangeRate.FromCurrencyCode, exchangeRate.ToCurrencyCode);
            getFromEntityTask = _forexRateRepository
                .FindByIdAsync(_mapper.Map<ForexRateKey>(exchangeRate));

            await Task.WhenAll(getCurrencyExchangeRatesTask, getFromEntityTask);

            result = ConvertAndSave(
                getCurrencyExchangeRatesTask.Result,
                getFromEntityTask.Result);
        }
        catch(Exception ex)
        {
            _logger.LogError("Caught Exception : {ex}", ex);

            var getFromEntityException = getFromEntityTask.Exception?.InnerException;
            var getCurrencyExchangeRatesException = getCurrencyExchangeRatesTask.Exception?.InnerException;
            if (getFromEntityException != default)
            {
                throw getFromEntityException;
            }
            if (getCurrencyExchangeRatesException != default)
            {
                result = TryRecoverFromFaultyRate(getFromEntityTask.Result, getCurrencyExchangeRatesException);
            }
            else
            {
                throw;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the cached/stored Forex Rate. If it has expired or does not exists, tries to get from the source.
    /// </summary>
    public async Task<ExchangeRateResponseDto> GetForexStoredRate(ExchangeRateRequestDto exchangeRate)
    {
        ExchangeRateResponseDto? result;

        ForexRateEntity? entity = await _forexRateRepository.FindByIdAsync(
            _mapper.Map<ForexRateKey>(exchangeRate));

        if (entity == default || HasExpired(entity))
        {
            try
            {
                var response = await _forexClient
                    .GetCurrencyExchangeRates(exchangeRate.FromCurrencyCode, exchangeRate.ToCurrencyCode);
                result = ConvertAndSave(response, entity);
            }
            catch (Exception ex)
            {
                return TryRecoverFromFaultyRate(entity, ex);
            }
        }
        else
        {
            result = _mapper.Map<ExchangeRateResponseDto>(entity);
        }

        return result;
    }

    /// <summary>
    /// Prepares a rate for storage.
    /// </summary>
    private ExchangeRateResponseDto ConvertAndSave(GetCurrencyExchangeRatesResponse? response, ForexRateEntity? entity)
    {
        ExchangeRateResponseDto? result = _mapper.Map<ExchangeRateResponseDto>(response);
        if (entity != default)
        {
            result = result with
            {
                CreatedAt = entity.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };
        }
        else
        {
            result = result with
            {
                CreatedAt = DateTime.UtcNow
            };
        }

        EnqueueUpdate(result, entity?.LastRefreshed);
        return result;
    }

    /// <summary>
    /// Fallback operations for recovering from a source error.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="getCurrencyExchangeRatesTask"></param>
    /// <returns></returns>
    /// <exception cref="InvalidExchangeRateException"></exception>
    private ExchangeRateResponseDto TryRecoverFromFaultyRate(
        ForexRateEntity? entity, Exception? getCurrencyExchangeRatesTask)
    {
        switch (getCurrencyExchangeRatesTask)
        {
            case HttpClientLimitReachedException limitException:
                if (entity != default)
                {
                    _logger.LogWarning($"Could not get the latest version from database. Return last known record.");
                    return _mapper.Map<ExchangeRateResponseDto>(entity);
                }
                throw limitException;

            case BadHttpRequestException badrequestException:
                throw new InvalidExchangeRateException(badrequestException);

            default:
                throw getCurrencyExchangeRatesTask!;
        }
    }

    /// <summary>
    /// Checks if the Rate has expired.
    /// </summary>
    private bool HasExpired(ForexRateEntity entity)
    {
        var lastUpdateDate = entity?.UpdatedAt ?? entity!.CreatedAt;
        var expirationDate = lastUpdateDate.AddMinutes(Settings.ExchangeRate.ExpirationMinutes);
        return DateTime.UtcNow >= expirationDate;
    }

    /// <summary>
    /// Enqueues the rate update procedure into a background job, so the request doesn't need to wait for updating.
    /// </summary>
    private void EnqueueUpdate(ExchangeRateResponseDto rateExchange, DateTime? lastKnownRefreshedDate)
    {
        _backgroundJobClient.Enqueue<IExchangeRateService>(x => x.UpdateForexRate(rateExchange, lastKnownRefreshedDate));
    }
}
