using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tickets_core.src.Contracts;
using tickets_core.src.Handlers;
using tickets_core.src.Services;

namespace tickets_core.src.Controllers;

[Authorize]
public sealed class TicketController(TicketService ticketService) : ApiControllerBase
{
    [HttpGet("/api/tickets")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TicketsByState>>>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var tickets = await ticketService.GetGroupedByStateAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TicketsByState>>.Ok(tickets, HttpContext.TraceIdentifier));
    }

    [HttpPost("/api/tickets")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TicketResponse>>> CreateAsync(
        [FromBody] CreateTicketRequest request, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.Failure("La sesión no contiene un usuario válido.", HttpContext.TraceIdentifier));

        var ticket = await ticketService.CreateAsync(userId, request, cancellationToken);
        return Ok(ApiResponse<TicketResponse>.Ok(ticket, HttpContext.TraceIdentifier));
    }

    [HttpGet("/api/tickets/deleted")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TicketResponse>>>> GetDeletedTicketsAsync(
        CancellationToken cancellationToken = default)
    {
        var tickets = await ticketService.GetDeletedTicketsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TicketResponse>>.Ok(tickets, HttpContext.TraceIdentifier));
    }

    [HttpGet("/api/tickets/{ticketId:guid}")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TicketDetails>>> GetTicketByIdAsync(
        Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await ticketService.GetTicketByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
            return NotFound(ApiResponse<object>.Failure("El ticket no fue encontrado.", HttpContext.TraceIdentifier));

        return Ok(ApiResponse<TicketDetails>.Ok(ticket, HttpContext.TraceIdentifier));
    }

    [HttpPut("/api/tickets/{ticketId:guid}")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> EditAsync(
        Guid ticketId, [FromBody] EditTicketRequest request, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.Failure("La sesión no contiene un usuario válido.", HttpContext.TraceIdentifier));

        await ticketService.EditAsync(ticketId, userId, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, HttpContext.TraceIdentifier));
    }

    [HttpPut("/api/tickets/{ticketId:guid}/state")]
    [Authorize(Roles = "Technician,Admin")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> ChangeStateAsync(
        Guid ticketId, [FromBody] ChangeTicketStateRequest request, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.Failure("La sesión no contiene un usuario válido.", HttpContext.TraceIdentifier));

        await ticketService.ChangeStateAsync(ticketId, userId, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, HttpContext.TraceIdentifier));
    }

    [HttpDelete("/api/tickets/{ticketId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(
        Guid ticketId, [FromBody] DeleteTicketRequest request, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.Failure("La sesión no contiene un usuario válido.", HttpContext.TraceIdentifier));

        await ticketService.DeleteAsync(ticketId, userId, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, HttpContext.TraceIdentifier));
    }
}
