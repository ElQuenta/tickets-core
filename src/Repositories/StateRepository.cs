using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using tickets_core.Infrastructure.Models;
using tickets_core.Infrastructure.Persistence;
using tickets_core.src.Contracts;

namespace tickets_core.src.Repositories
{
    public class StateRepository(ApplicationDbContext context)
    {
        public async Task<IReadOnlyList<State>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await context.States
                .AsNoTracking()
                .ToListAsync(cancellationToken);
    }
}