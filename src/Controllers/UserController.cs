using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tickets_core.src.Contracts;
using tickets_core.src.Handlers;
using tickets_core.src.Services;

namespace tickets_core.src.Controllers;

[Authorize(Roles = "Admin")]
public sealed class UserController(UserService service) : ApiControllerBase
{
    [HttpGet("/api/users")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserResponse>>>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<IReadOnlyList<UserResponse>>.Ok(await service.GetAllAsync(cancellationToken),
            HttpContext.TraceIdentifier));

    [HttpPut("/api/users/{userId:guid}")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserResponse>>> EditAsync(Guid userId,
        [FromBody] EditUserRequest request, CancellationToken cancellationToken = default)
    {
        var result = await service.EditAsync(userId, request, cancellationToken);
        if (result.Errors is not null)
            return BadRequest(ApiResponse<object>.Failure("La solicitud contiene errores de validación.",
                HttpContext.TraceIdentifier, result.Errors));

        return Ok(ApiResponse<UserResponse>.Ok(result.Response, HttpContext.TraceIdentifier));
    }
}
