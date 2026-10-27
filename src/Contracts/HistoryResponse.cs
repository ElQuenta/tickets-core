namespace tickets_core.src.Contracts;

public sealed record HistoryResponse(Guid Id, Guid TicketId, Guid UserId, string Comment,
    string PrevState, string NewState, DateTime CreatedAt, DateTime UpdatedAt);
