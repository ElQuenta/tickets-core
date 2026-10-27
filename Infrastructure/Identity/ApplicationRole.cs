using Microsoft.AspNetCore.Identity;

namespace tickets_core.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ApplicationRole() => Id = Guid.NewGuid();
}
