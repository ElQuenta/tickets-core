using tickets_core.Infrastructure.Models;
using tickets_core.src.Contracts;

namespace tickets_core.src.Services;

internal static class TicketMappings
{
    public static TicketResponse ToResponse(this Ticket ticket) => new(ticket.Id,
        ticket.UserId, ticket.StateId, ticket.Title, ticket.Description, ticket.Equipment,
        ticket.CreatedAt, ticket.UpdatedAt, ticket.DeletedAt);

    public static HistoryResponse ToResponse(this History history) => new(history.Id,
        history.TicketId, history.UserId, history.Comment, history.PrevState,
        history.NewState, history.CreatedAt, history.UpdatedAt);
}
