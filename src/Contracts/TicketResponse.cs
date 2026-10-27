namespace tickets_core.src.Contracts;

public sealed record TicketResponse(Guid Id, Guid UserId, Guid StateId, string Title,
    string Description, DateTime CreatedAt, DateTime UpdatedAt, DateTime? DeletedAt);
