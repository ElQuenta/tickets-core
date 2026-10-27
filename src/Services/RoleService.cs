using tickets_core.Infrastructure.Identity;
using tickets_core.src.Contracts;
using tickets_core.src.Repositories;

namespace tickets_core.src.Services;

public sealed class RoleService(RoleRepository repository)
{
    public async Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roles = await repository.GetAllAsync(cancellationToken);
        return roles.Select(ToResponse).ToList();
    }

    internal static RoleResponse ToResponse(ApplicationRole role) =>
        new(role.Id, role.Name!, role.CreatedAt, role.UpdatedAt);
}
