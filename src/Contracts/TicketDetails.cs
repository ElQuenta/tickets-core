namespace tickets_core.src.Contracts;

public sealed record TicketDetails(TicketResponse Ticket, IReadOnlyList<HistoryResponse> History);
