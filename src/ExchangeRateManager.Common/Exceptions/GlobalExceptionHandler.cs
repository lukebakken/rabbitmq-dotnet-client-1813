using ExchangeRateManager.Common.Extensions;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ExchangeRateManager.Common.Exceptions;

/// <summary>
/// The Global Exception handler. Handles all handled and unhandled exceptions,
/// converting them into a ProblemDetails, RFC7807 compliant response.
/// </summary>
public class GlobalExceptionHandler(
    IWebHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;
    private readonly IWebHostEnvironment _environment = environment;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problemDetails;

        if (exception is ProblemDetailsException problem)
        {
            _logger.LogWarning(exception, "A handled exception occurred {Message}", exception.Message);
            problemDetails = problem.Details;
            httpContext.Response.StatusCode = problem.Details.Status
                ?? StatusCodes.Status500InternalServerError;
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception occurred {Message}", exception.Message);
            problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server Error",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                Detail = "Unhandled exception occurred. Please Contact the service administrator or Create a ticket on the Issue Tracker."
            };
        }

        problemDetails.Extensions.Add("Request Id:", httpContext.TraceIdentifier);

        if (!_environment.IsHighLevel())
        {
            problemDetails.Extensions.Add("Exception:", exception.GetType().FullName);
            problemDetails.Extensions.Add("Source:", exception.Source);
            problemDetails.Extensions.Add("TargetSite:", exception.TargetSite?.ToString());
            problemDetails.Extensions.Add("StackTrace:", exception.StackTrace);
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, CancellationToken.None);
        return true;
    }
}
