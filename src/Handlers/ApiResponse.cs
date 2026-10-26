namespace tickets_core.src.Handlers;

public interface IApiResponse;

public sealed record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data,
    IReadOnlyDictionary<string, string[]>? Errors,
    string TraceId) : IApiResponse
{
    public static ApiResponse<T> Ok(T? data, string traceId, string message = "Solicitud completada.")
        => new(true, message, data, null, traceId);

    public static ApiResponse<T> Failure(string message, string traceId,
        IReadOnlyDictionary<string, string[]>? errors = null)
        => new(false, message, default, errors, traceId);
}
