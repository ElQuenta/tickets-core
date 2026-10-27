using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tickets_core.src.Contracts;
using tickets_core.src.Handlers;
using tickets_core.src.Services;

namespace tickets_core.src.Controllers;

[Authorize]
public sealed class StateController(StateService stateService) : ApiControllerBase
{
    [HttpGet("/api/states")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StateResponse>>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var states = await stateService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<StateResponse>>.Ok(states, HttpContext.TraceIdentifier));
    }
}
