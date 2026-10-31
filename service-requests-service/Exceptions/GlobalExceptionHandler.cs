using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace service_requests_service.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetails,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            UnauthorizedTransitionException => (409, "Invalid status transition"),
            KeyNotFoundException => (404, "Resource not found"),
            ArgumentException => (400, "Invalid request"),
            BadHttpRequestException ex => (ex.StatusCode, "Invalid HTTP request"),
            _ => (500, "Internal server error")
        };

        if (status >= 500)
            _logger.LogError(exception, "Something goes wrong... {TraceId}", context.TraceIdentifier);
        
        context.Response.StatusCode = status;
        var pdContext = new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status >= 500
                    ? "An unexpected error occurred."
                    : exception.Message
            },
            Exception = exception
        };

        return await _problemDetails.TryWriteAsync(pdContext);
    }
}