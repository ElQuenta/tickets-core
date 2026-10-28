using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using tickets_core.Infrastructure.Persistence;


namespace tickets_core.src.Repositories
{
    public sealed class UserRoleRepository(ApplicationDbContext context)
    {
        public async Task CreateUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        {
            if (!await context.Users.AnyAsync(user => user.Id == userId, cancellationToken))
                throw new KeyNotFoundException("El usuario no existe.");

            if (!await context.Roles.AnyAsync(role => role.Id == roleId, cancellationToken))
                throw new KeyNotFoundException("El rol no existe.");

            if (await AssignmentExistsAsync(userId, roleId, cancellationToken))
                return;

            var userRole = new IdentityUserRole<Guid>
            {
                UserId = userId,
                RoleId = roleId
            };
            context.UserRoles.Add(userRole);
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // Otra solicitud puede haber insertado la misma clave compuesta.
                context.Entry(userRole).State = EntityState.Detached;
                if (!await AssignmentExistsAsync(userId, roleId, cancellationToken))
                    throw;
            }
        }

        public async Task<bool> DeleteUserRoleAsync(Guid userId, Guid roleId,
            CancellationToken cancellationToken = default)
        {
            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

                var existingUserId = await context.Users
                    .FromSqlInterpolated($"SELECT * FROM [dbo].[AspNetUsers] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {userId}")
                    .AsNoTracking()
                    .Select(user => (Guid?)user.Id)
                    .SingleOrDefaultAsync(cancellationToken);
                if (existingUserId is null)
                    throw new KeyNotFoundException("El usuario no existe.");

                var roleIds = await context.UserRoles.Where(assignment => assignment.UserId == userId)
                    .Select(assignment => assignment.RoleId).ToListAsync(cancellationToken);
                if (!roleIds.Contains(roleId))
                    throw new KeyNotFoundException("El usuario no tiene asignado ese rol.");

                if (roleIds.Count <= 1)
                    return false;

                await context.UserRoles.Where(assignment => assignment.UserId == userId && assignment.RoleId == roleId)
                    .ExecuteDeleteAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return true;
            });
        }

        private Task<bool> AssignmentExistsAsync(Guid userId, Guid roleId, CancellationToken cancellationToken) =>
            context.UserRoles.AnyAsync(assignment => assignment.UserId == userId && assignment.RoleId == roleId,
                cancellationToken);
    }
}
