using Microsoft.AspNetCore.Identity;
using tickets_core.Infrastructure.Models;

namespace tickets_core.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser() => Id = Guid.NewGuid();

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<History> Histories { get; set; } = new List<History>();
}
