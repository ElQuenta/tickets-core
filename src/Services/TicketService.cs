using Microsoft.AspNetCore.Identity;
using tickets_core.Infrastructure.Identity;
using tickets_core.src.Contracts;
using tickets_core.src.Repositories;

namespace tickets_core.src.Services;

public sealed class TicketService(TicketRepository ticketRepository, HistoryRepository historyRepository,
    UserManager<ApplicationUser> userManager)
{
    private readonly TicketRepository _ticketRepository = ticketRepository;
    private readonly HistoryRepository _historyRepository = historyRepository;

    public Task<IReadOnlyList<TicketsByState>> GetGroupedByStateAsync(
        CancellationToken cancellationToken = default) =>
        _ticketRepository.GetGroupedByStateAsync(cancellationToken);

    public async Task<TicketResponse> CreateAsync(Guid userId, CreateTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.CreateAsync(userId, request.Title, request.Description, cancellationToken);
        return ticket.ToResponse();
    }

    public async Task ChangeStateAsync(Guid ticketId, Guid userId, ChangeTicketStateRequest request,
        CancellationToken cancellationToken = default) =>
        await _ticketRepository.ChangeStateAsync(ticketId, request.StateId, userId, request.Comment, cancellationToken);
    
    public async Task EditAsync(Guid ticketId, Guid userId, EditTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetTicketByIdAsync(ticketId, cancellationToken);
        if (ticket is null || ticket.DeletedAt is not null)
            throw new KeyNotFoundException("El ticket no fue encontrado.");
        if (ticket.UserId != userId)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null || !await userManager.IsInRoleAsync(user, "Admin"))
                throw new UnauthorizedAccessException("Solo el administrador o el creador pueden editar el ticket.");
        }

        await _ticketRepository.EditAsync(ticketId, userId, request.Title, request.Description, request.Comment, cancellationToken);
    }
    
    public async Task<TicketDetails?> GetTicketByIdAsync(Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetTicketByIdAsync(ticketId, cancellationToken);
        if (ticket is null)
            return null;

        var history = await _historyRepository.GetByTicketIdAsync(ticketId, cancellationToken);
        return new TicketDetails(ticket.ToResponse(), history.Select(entry => entry.ToResponse()).ToList());
    }

    public async Task DeleteAsync(Guid ticketId, Guid userId, DeleteTicketRequest request,
        CancellationToken cancellationToken = default) =>
        await _ticketRepository.DeleteAsync(ticketId, userId, request.Comment, cancellationToken);

    public async Task<IReadOnlyList<TicketResponse>> GetDeletedTicketsAsync(
        CancellationToken cancellationToken = default)
    {
        var tickets = await _ticketRepository.getDeletedTicketsAsync(cancellationToken);
        return tickets.Select(ticket => ticket.ToResponse()).ToList();
    }
}
