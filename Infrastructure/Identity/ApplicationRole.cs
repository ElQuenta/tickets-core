using Microsoft.AspNetCore.Identity;

namespace tickets_core.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() => Id = Guid.NewGuid();
}
