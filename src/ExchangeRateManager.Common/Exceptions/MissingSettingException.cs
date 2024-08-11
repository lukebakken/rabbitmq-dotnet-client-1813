using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace ExchangeRateManager.Common.Exceptions;

[Serializable]
[ExcludeFromCodeCoverage(Justification = "Exception. No actual executable code.")]
public class MissingSettingException(string fullKeyName) : ProblemDetailsException(
    StatusCodes.Status500InternalServerError, $"Missing '{fullKeyName}' on appsettings.json")
{ }