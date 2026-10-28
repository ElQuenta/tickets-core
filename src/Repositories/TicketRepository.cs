using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using tickets_core.Infrastructure.Models;
using tickets_core.Infrastructure.Persistence;
using tickets_core.src.Contracts;

namespace tickets_core.src.Repositories;

public sealed class TicketRepository(ApplicationDbContext context)
{
    public async Task<Ticket> CreateAsync(Guid userId, string title, string description, string equipment,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(equipment);

        if (!await context.Users.AnyAsync(user => user.Id == userId, cancellationToken))
            throw new InvalidOperationException("El usuario no existe.");

        var stateId = await context.States
            .Where(state => state.Name == "Pendiente")
            .Select(state => (Guid?)state.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("El estado Pendiente no existe. Ejecuta los seeds.");

        var ticket = new Ticket
        {
            UserId = userId,
            StateId = stateId,
            Title = title,
            Description = description,
            Equipment = equipment
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    public async Task<Ticket?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken = default) =>
        await context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == ticketId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Ticket>> getDeletedTicketsAsync(
        CancellationToken cancellationToken = default) =>
        await context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.DeletedAt != null)
            .ToListAsync(cancellationToken);

    public async Task ChangeStateAsync(Guid ticketId, Guid stateId, Guid userId, string? comment = null,
        CancellationToken cancellationToken = default) =>
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            EXEC dbo.SP_ChangeTicketState @Ticket_id={ticketId}, @State_id={stateId},
                @User_id={userId}, @Comment={comment}
            """, cancellationToken);

    public async Task EditAsync(Guid ticketId, Guid userId, string? title = null,
        string? description = null, string? comment = null, string? equipment = null,
        CancellationToken cancellationToken = default) =>
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            EXEC dbo.SP_EditTicket @Ticket_id={ticketId}, @User_id={userId},
                @Title={title}, @Description={description}, @Comment={comment}, @Equipment={equipment}
            """, cancellationToken);

    public async Task DeleteAsync(Guid ticketId, Guid userId, string? comment,
        CancellationToken cancellationToken = default) =>
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            EXEC dbo.SP_DeleteTicket @Ticket_id={ticketId}, @User_id={userId}, @Comment={comment}
            """, cancellationToken);

    public async Task<IReadOnlyList<TicketsByState>> GetGroupedByStateAsync(
        CancellationToken cancellationToken = default)
    {
        var groups = await context.Database
            .SqlQuery<TicketsByStateRow>($"EXEC dbo.SP_GetTicketsGroupedByState")
            .ToListAsync(cancellationToken);

        return groups.Select(group => new TicketsByState(group.StateId, group.StateName,
            JsonSerializer.Deserialize<List<TicketSummary>>(group.Tickets)
                ?? throw new JsonException("El procedimiento no devolvió una lista de tickets válida.")))
            .ToList();
    }

    private sealed class TicketsByStateRow
    {
        public Guid StateId { get; set; }
        public string StateName { get; set; } = string.Empty;
        public string Tickets { get; set; } = string.Empty;
    }
}
