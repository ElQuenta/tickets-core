using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tickets_core.src.Contracts;
using tickets_core.src.Handlers;
using tickets_core.src.Services;

namespace tickets_core.src.Controllers;

[Authorize(Roles = "Admin")]
public sealed class UserRoleController(UserRoleService service) : ApiControllerBase
{
    [HttpPost("/api/users/{userId:guid}/roles")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserRoleResponse>>> CreateAsync(Guid userId,
        [FromBody] CreateUserRoleRequest request, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<UserRoleResponse>.Ok(await service.CreateAsync(userId, request, cancellationToken),
            HttpContext.TraceIdentifier));
}
