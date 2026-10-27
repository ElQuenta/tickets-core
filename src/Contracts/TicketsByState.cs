using System.Text.Json.Serialization;

namespace tickets_core.src.Contracts;

public sealed record TicketsByState(Guid StateId, string StateName, IReadOnlyList<TicketSummary> Tickets);

public sealed record TicketSummary(
    [property: JsonPropertyName("ID")] Guid Id,
    [property: JsonPropertyName("User_id")] Guid UserId,
    [property: JsonPropertyName("State_id")] Guid StateId,
    string Title,
    string Description,
    DateTime CreatedAt,
    DateTime UpdatedAt);
