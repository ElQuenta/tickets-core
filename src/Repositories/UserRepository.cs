using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using tickets_core.Infrastructure.Identity;
using tickets_core.Infrastructure.Persistence;
using tickets_core.src.Contracts;

namespace tickets_core.src.Repositories;

public sealed class UserRepository(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
{
    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await GetResponses(context.Users.AsNoTracking().OrderBy(user => user.UserName).ThenBy(user => user.Id))
            .ToListAsync(cancellationToken);

    public Task<UserResponse> GetResponseAsync(Guid userId, CancellationToken cancellationToken = default) =>
        GetResponses(context.Users.AsNoTracking().Where(user => user.Id == userId)).SingleAsync(cancellationToken);

    public Task<ApplicationUser?> FindAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public async Task<IdentityResult> UpdateAsync(ApplicationUser user, string? password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (user.PhoneNumber is not null && await context.Users.AnyAsync(
                other => other.Id != user.Id && other.PhoneNumber == user.PhoneNumber, cancellationToken))
            return IdentityResult.Failed(new IdentityError
            {
                Code = "DuplicatePhoneNumber", Description = "El teléfono ya pertenece a otro usuario."
            });

        try
        {
            // Ambas rutas validan y normalizan el usuario y realizan una sola escritura.
            if (password is not null)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                return await userManager.ResetPasswordAsync(user, token, password);
            }

            user.SecurityStamp = Guid.NewGuid().ToString();
            return await userManager.UpdateAsync(user);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "DuplicateUserData",
                Description = "El nombre de usuario, correo o teléfono ya pertenece a otro usuario."
            });
        }
    }

    private IQueryable<UserResponse> GetResponses(IQueryable<ApplicationUser> query) =>
        query.Select(user => new UserResponse(
            user.Id, user.UserName, user.Email, user.EmailConfirmed,
            user.PhoneNumber, user.PhoneNumberConfirmed, user.LockoutEnabled,
            user.LockoutEnd, user.AccessFailedCount, user.CreatedAt, user.UpdatedAt,
            (from assignment in context.UserRoles
             join role in context.Roles on assignment.RoleId equals role.Id
             where assignment.UserId == user.Id
             orderby role.Name
             select role.Name!).ToList()));
}
