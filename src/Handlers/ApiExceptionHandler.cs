using Microsoft.AspNetCore.Diagnostics;

namespace tickets_core.src.Handlers;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error no controlado. TraceId: {TraceId}", httpContext.TraceIdentifier);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.Failure("Ocurrió un error interno.", httpContext.TraceIdentifier),
            cancellationToken);
        return true;
    }
}
