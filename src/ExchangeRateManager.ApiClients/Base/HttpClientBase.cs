using ExchangeRateManager.Common.Exceptions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace ExchangeRateManager.ApiClients.Base;

[ExcludeFromCodeCoverage(Justification = "API CLient code. Untestable")]
public abstract class HttpClientBase(string clientName, HttpClient httpClient, ILogger logger)
{
    public string ClientName => clientName;
    protected readonly ILogger _logger = logger;
    protected readonly HttpClient _httpClient = httpClient;

    protected async Task<T?> HandleResponse<T>(Func<Task<HttpResponseMessage>> callback, [CallerMemberName] string callerMethod = default!)
    {
        try
        {
            var response = await callback();
            if (response.StatusCode >= HttpStatusCode.BadRequest)
            {
                _logger.LogError("[{callerClass}] '{callerMethod}' failed to provide data from {clientName}.",
                    GetType().Name, callerMethod, ClientName);

                _logger.LogDebug("[{callerClass}] '{callerMethod}' faulty Response: '{faultResponse}'",
                    GetType().Name, callerMethod, await response.Content.ReadAsStringAsync());

                throw new HttpClientException(ClientName);
            } 

            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (HttpClientException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{callerClass}] '{callerMethod}' failed from an unexpected error while trying to consume {clientName}.",
                GetType().Name, callerMethod, ClientName);
            _logger.LogError(ex, "[{callerClass}] '{callerMethod}' exception details: {exceptionMessage}.",
                GetType().Name, callerMethod, ex.Message);

            throw new HttpClientException(ClientName, ex);
        }
    }
}