using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Ari.UrlShortener.Infrastructure;

/// <summary>
/// Catches any unhandled exception, logs it through NLog, and returns a 500
/// ProblemDetails response. Stack traces are never included in the response.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetails;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails)
    {
        _logger = logger;
        _problemDetails = problemDetails;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Unhandled exception. Method: {Method} Path: {Path} TraceId: {TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                Extensions = { ["traceId"] = httpContext.TraceIdentifier },
            },
        });
    }
}
