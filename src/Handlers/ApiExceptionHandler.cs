using Microsoft.AspNetCore.Diagnostics;

namespace tickets_core.src.Handlers;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, exception.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error interno.")
        };
        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Error no controlado. TraceId: {TraceId}", httpContext.TraceIdentifier);
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.Failure(message, httpContext.TraceIdentifier),
            cancellationToken);
        return true;
    }
}
