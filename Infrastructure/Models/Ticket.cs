using tickets_core.Infrastructure.Identity;

namespace tickets_core.Infrastructure.Models;

public sealed class Ticket
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid StateId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public State State { get; set; } = null!;
    public ICollection<History> Histories { get; set; } = new List<History>();
}
