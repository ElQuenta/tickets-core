using tickets_core.src.Contracts;
using tickets_core.src.Repositories;

namespace tickets_core.src.Services;

public sealed class UserRoleService(UserRoleRepository repository)
{
    public Task<bool> DeleteAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default) =>
        repository.DeleteUserRoleAsync(userId, roleId, cancellationToken);

    public async Task<UserRoleResponse> CreateAsync(Guid userId, CreateUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        await repository.CreateUserRoleAsync(userId, request.RoleId, cancellationToken);
        return new UserRoleResponse(userId, request.RoleId);
    }
}
