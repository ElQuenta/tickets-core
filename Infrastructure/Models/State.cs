namespace tickets_core.Infrastructure.Models;

public sealed class State
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
