using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Http;

namespace ExchangeRateManager.Common.Exceptions;

[Serializable]
public class InvalidExchangeRateException(Exception innerException) : ProblemDetailsException(
    StatusCodes.Status400BadRequest,
    "Either 'FromCurrency' or 'ToCurrency' are not recognized as valid codes.",
    innerException) { }
