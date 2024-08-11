using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace ExchangeRateManager.Common.Exceptions;

[Serializable]
[ExcludeFromCodeCoverage(Justification = "Untestable code")]
public class HttpClientException : ProblemDetailsException
{
    public HttpClientException(string apiProviderName) :
        base(StatusCodes.Status500InternalServerError, $"Failed to provide data from {apiProviderName}. Check Logs.")
    { }

    public HttpClientException(string apiProviderName, Exception innerException) :
        base(StatusCodes.Status500InternalServerError, $"Failed to provide data from {apiProviderName}. Check Logs.", innerException)
    { }
}