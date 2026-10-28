using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tickets_core.src.Contracts;
using tickets_core.src.Handlers;
using tickets_core.src.Services;

namespace tickets_core.src.Controllers;

[Authorize(Roles = "Admin")]
public sealed class UserRoleController(UserRoleService service) : ApiControllerBase
{
    [HttpDelete("/api/users/{userId:guid}/roles/{roleId:guid}")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserRoleResponse>>> DeleteAsync(Guid userId, Guid roleId,
        CancellationToken cancellationToken = default)
    {
        if (!await service.DeleteAsync(userId, roleId, cancellationToken))
            return Conflict(ApiResponse<object>.Failure(
                "No se puede quitar el último rol del usuario. Debe conservar al menos un rol.",
                HttpContext.TraceIdentifier));

        return Ok(ApiResponse<UserRoleResponse>.Ok(new UserRoleResponse(userId, roleId),
            HttpContext.TraceIdentifier));
    }

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
