using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace tickets_core.src.Handlers;

public sealed class ApiResponseFilter : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        var traceId = context.HttpContext.TraceIdentifier;
        if (context.Result is ObjectResult result && result.Value is not IApiResponse)
        {
            var status = result.StatusCode ?? StatusCodes.Status200OK;
            result.Value = status < 400
                ? ApiResponse<object>.Ok(result.Value, traceId)
                : ApiResponse<object>.Failure(
                    Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(status), traceId);
            result.DeclaredType = typeof(ApiResponse<object>);
        }
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
