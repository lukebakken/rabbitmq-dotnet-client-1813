
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Http;

namespace ExchangeRateManager.Common.Exceptions
{
    [Serializable]
    public class HttpClientLimitReachedException(string apiProviderName) : ProblemDetailsException(
        StatusCodes.Status402PaymentRequired,
        $"The available call limit for {apiProviderName} " +
            $"has been reached, and there is no available stored " +
            $"record. Please retry calling this endpoint later.") { }
}