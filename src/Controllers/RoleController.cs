using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tickets_core.src.Contracts;
using tickets_core.src.Handlers;
using tickets_core.src.Services;

namespace tickets_core.src.Controllers;

[Authorize(Roles = "Admin")]
public sealed class RoleController(RoleService service) : ApiControllerBase
{
    [HttpGet("/api/roles")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleResponse>>>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<IReadOnlyList<RoleResponse>>.Ok(await service.GetAllAsync(cancellationToken),
            HttpContext.TraceIdentifier));
}
