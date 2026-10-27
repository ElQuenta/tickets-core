using Microsoft.EntityFrameworkCore;
using tickets_core.Infrastructure.Identity;
using tickets_core.Infrastructure.Persistence;

namespace tickets_core.src.Repositories
{
    public sealed class RoleRepository(ApplicationDbContext context)
    {
        public async Task<IReadOnlyList<ApplicationRole>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var roles = await context.Roles.AsNoTracking().OrderBy(role => role.Name).ToListAsync(cancellationToken);
            return roles;
        }
    }
}
