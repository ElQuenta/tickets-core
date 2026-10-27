using Microsoft.EntityFrameworkCore;
using tickets_core.Infrastructure.Models;
using tickets_core.Infrastructure.Persistence;

namespace tickets_core.src.Repositories;

public sealed class HistoryRepository(ApplicationDbContext context)
{
    public async Task<IReadOnlyList<History>> GetByTicketIdAsync(
        Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await context.Histories
            .AsNoTracking()
            .Where(history => history.TicketId == ticketId)
            .OrderBy(history => history.CreatedAt)
            .ThenBy(history => history.Id)
            .ToListAsync(cancellationToken);
    }
}
