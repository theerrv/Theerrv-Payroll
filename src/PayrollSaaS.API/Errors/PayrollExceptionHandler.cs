using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PayrollSaaS.API.Errors;

/// <summary>
/// IExceptionHandler → RFC 7807 ProblemDetails for every unhandled exception.
/// Domain InvalidOperationException (lifecycle violations) → 409 Conflict.
/// </summary>
public class PayrollExceptionHandler : IExceptionHandler
{
    private readonly ILogger<PayrollExceptionHandler> _logger;

    public PayrollExceptionHandler(ILogger<PayrollExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            InvalidOperationException ex => (StatusCodes.Status409Conflict, "Conflict", ex.Message),
            UnauthorizedAccessException ex => (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),
            KeyNotFoundException ex => (StatusCodes.Status404NotFound, "Not Found", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error",
                  httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                      ? exception.Message
                      : "An unexpected error occurred.")
        };

        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.io/{statusCode}"
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
