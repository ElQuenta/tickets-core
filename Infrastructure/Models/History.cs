using tickets_core.Infrastructure.Identity;

namespace tickets_core.Infrastructure.Models;

public sealed class History
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Guid UserId { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string PrevState { get; set; } = string.Empty;
    public string NewState { get; set; } = string.Empty;
    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
