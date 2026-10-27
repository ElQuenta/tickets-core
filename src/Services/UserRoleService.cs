using tickets_core.src.Contracts;
using tickets_core.src.Repositories;

namespace tickets_core.src.Services;

public sealed class UserRoleService(UserRoleRepository repository)
{
    public async Task<UserRoleResponse> CreateAsync(Guid userId, CreateUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        await repository.CreateUserRoleAsync(userId, request.RoleId, cancellationToken);
        return new UserRoleResponse(userId, request.RoleId);
    }
}
